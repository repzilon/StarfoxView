using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaPanNZoom.CustomControls;
#if NETFRAMEWORK
using StarFox.Interop;
#endif
using StarFox.Interop.MAP;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;
using StarwingMapVisualizer.Controls.Subcontrols;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Misc;
using Path = System.IO.Path;

namespace StarwingMapVisualizer.Controls
{
	public interface IPausable
	{
		bool Paused { get; }
		void Pause();
		void Unpause();
	}

	/// <summary>
	/// Interaction logic for MAPControl.xaml
	/// </summary>
	public partial class MapControl : UserControl, IPausable
	{
		public bool Paused { get; private set; }

		public MapControl()
		{
			InitializeComponent();
			MAPTabViewer.Items.Clear();
		}

		private Dictionary<MAPScript, MAP_FINST> _tabMap = new Dictionary<MAPScript, MAP_FINST>();

		private MAP_FINST CurrentState
		{
			get
			{
				if (this.SelectedScript == null) {
					return null;
				}

				_tabMap.TryGetValue(this.SelectedScript, out var val);
				return val;
			}
		}

		private MAPScript SelectedScript
		{
			get { return (MAPTabViewer.SelectedItem as TabItem)?.Tag as MAPScript; }
		}

		private MAPContextDefinition _selectedContext;

		/*TODO : 3D VIEWER VARS
		private MAP3DControl _mapWindow;
		private bool MapWindowOpened => _mapWindow != default;
		// */

		public void Pause() => Paused = true;

		public void Unpause()
		{
			Paused = false;
			_      = InvalidateFiles();
		}

		/// <summary>
		/// Forces control to check <see cref="AppResources.OpenFiles"/> for any map files
		/// </summary>
		public async Task InvalidateFiles()
		{
			foreach (var file in AppResources.OpenMAPFiles)
				await OpenFile(file);
		}

		private async void MapExportButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.SelectedScript == null) {
				return;
			}

			var fileName = Path.Combine(Environment.CurrentDirectory,
				"export", "maps", $"{this.SelectedScript.Header.LevelMacroName}.sfmap");
			var directory = Path.GetDirectoryName(fileName);
			if (!Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}

			using (var file = File.Create(fileName)) {
				this.SelectedScript.LevelData.Serialize(file);
			}

			if (await MessageBox.Show($"The map was successfully exported to:\n{fileName}\n" +
								$"Do you want to copy its location to the clipboard?", "Complete",
					MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
				await AvaloniaBridge.Clipboard().SetTextAsync(fileName);
			}
		}

		private async void View3DButton_Click(object sender, RoutedEventArgs e)
		{
			/* TODO : MAPControl.View3DButton_Click
			var map3D = _mapWindow = new MAP3DControl(this.SelectedScript);
			await map3D.ShowMapContents();
			map3D.Closed += delegate { // WINDOW CLOSED
				_mapWindow              = null;
				View3DButton.IsVisible  = true;
			};
			map3D.Show();
			//update the 3D editor view
			await SwitchEditorBackground();
			View3DButton.IsVisible  = false;
			// */
		}

		private void SetupPlayFieldHorizontal(Panel panCanvas, int layer, int time,
		IImmutableSolidColorBrush foreground, double levelEnd = 100000000, string startText = "DELAY",
		string endText = "FINISH", bool showTimes = true, int yOffset = 0)
		{
			TextBlock AddFieldText(string text, double x, double y, bool major = false)
			{
				var textControl = new TextBlock() {
					Text       = text,
					FontSize   = major ? 28 : 22,
					FontFamily = new FontFamily("Consolas"),
					Padding    = new Thickness(10, 5, 10, 5),
					Foreground = foreground
				};
				Canvas.SetTop(textControl, y);
				Canvas.SetLeft(textControl, x);
				// TODO : Panel.SetZIndex
				//Panel.SetZIndex(textControl, 0);
				return textControl;
			}

			double yPosition = layer * 100 + yOffset;

			Line delayLine = new Line() {
				StartPoint      = new Point(time, yPosition),
				EndPoint        = new Point(levelEnd, yPosition),
				StrokeThickness = 4,
				Stroke          = foreground,
				Fill            = foreground,
			};
			double textYPosition = yPosition + 25;

			panCanvas.Children.Add(delayLine);
			panCanvas.Children.Add(AddFieldText(startText, time + 50, textYPosition - 50));

			if (showTimes) {
				for (double x = time; x <= levelEnd; x += 100) {
					bool major = x % 1000 == 0;
					panCanvas.Children.Add(AddFieldText(time.ToString(), x, textYPosition, major));
					time += 100;
				}
			}

			panCanvas.Children.Add(AddFieldText(endText, levelEnd + 50, textYPosition - 50));
		}

		private void SetupMapLoops(MAPData loopLevelData, Panel panCanvas, int layer, int subMapEnterTime)
		{
			foreach (var loopRegion in loopLevelData.SectionMarkers.Values) {
				if (!loopRegion.IsLooped) {
					continue;
				}

				SetupPlayFieldHorizontal(panCanvas, layer, loopRegion.EstimatedTimeStart + subMapEnterTime,
					Brushes.Orange, loopRegion.EstimatedTimeEnd + subMapEnterTime, loopRegion.LabelName, "END LOOP",
					false, 20);
			}
		}

		private void MapContextButton_Click(object sender, RoutedEventArgs e)
		{
			LevelContextViewer viewer;
			if (this.SelectedScript == null || !this.SelectedScript.ReferencedContexts.Any()) {
				viewer = new LevelContextViewer(FILEStandard.MAPImport.LoadedContextDefinitions) {
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
				};
			} else {
				viewer = new LevelContextViewer(this.SelectedScript.ReferencedContexts.Select(x => x.Key).ToArray()) {
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
				};
			}

			viewer.EditorPreviewSelectionChanged += async (object sender2, MAPContextDefinition definition) => {
				await SwitchEditorBackground(definition);
			};
			viewer.Show(Application.Current.MainWindow());
		}

		IImmutableSolidColorBrush _mapNodeLineBrush = Brushes.Yellow;
		double _expandedX;
		int _treeLayer, _lastTime;
		double _lastX, _lastY;

		private async Task<int> EnumerateEvents(MAPScript file, Panel eventCanvas, int currentTime,
		bool autoDereference = true, int layer = 0)
		{
			double layerShift = layer * 100;

			_mapNodeLineBrush = layer == 0 ? Brushes.Yellow : Brushes.DeepSkyBlue;

			_lastY = layerShift;
			_lastX = currentTime;

			foreach (var evt in file.LevelData.Events) {
				//Adjust level timing
				if (evt is IMAPDelayEvent delay) {
					currentTime += delay.Delay;
				}

				evt.LevelDelay = currentTime;

				var control = new MapEventNodeControl(evt);
				eventCanvas.Children.Add(control);

				control.Measure(new Size(5000, 5000));

				double middleX   = currentTime;
				double rightEdge = middleX + control.DesiredSize.Width / 2;
				double leftEdge  = middleX - control.DesiredSize.Width / 2;

				_treeLayer++;

				if (leftEdge > _expandedX) {
					_treeLayer = 0;
					_lastX     = currentTime;
					_lastY     = layerShift;
				}

				if (rightEdge > _expandedX) {
					_expandedX = rightEdge;
				}

				if (Math.Abs(_lastX - middleX) > 40) {
					_lastX = currentTime;
					_lastY = layerShift;
				}

				Canvas.SetLeft(control, middleX - control.DesiredSize.Width / 2);

				double drawY = 200 + layerShift + (_treeLayer * 75);
				Canvas.SetTop(control, drawY);

				// TODO : Panel.SetZIndex in MAPControl.EnumerateEvents
				//Panel.SetZIndex(control, 1);

				Line delayLine = new Line() {
					StartPoint      = new Point(_lastX, _lastY),
					EndPoint        = new Point(middleX, drawY),
					StrokeThickness = 2,
					Stroke          = _mapNodeLineBrush,
					Fill            = _mapNodeLineBrush,
				};

				_lastX = middleX;
				_lastY = drawY;

				eventCanvas.Children.Add(delayLine);

				if (evt is MAPJSREvent mapjsr && autoDereference) {
					// SUBSECTION FOUND!! WE'RE ALLOWED TO INCLUDE IT
					MAPFile   subMap;
					MAPScript subScript = null;

					try {
						subScript = FILEStandard.GetMapScriptByMacroName(mapjsr.SubroutineName).Result;
					} catch { }

					if (subScript == null) {
						//Try to automatically find the subsection without getting the user involved
						if (FILEStandard.SearchProjectForFile($"{mapjsr.SubroutineName}.ASM", out var mapInfo)) {
							subMap = FILEStandard.OpenMAPFile(mapInfo).Result;
							if (subMap == null || !subMap.Scripts.TryGetValue(mapjsr.SubroutineName, out subScript)) {
								// FAILED! Couldn't open the map. Prompt the user for a new file
								subScript = null;
							}
						}

						//Loop while user selects the right file
						while (subScript == null) {
							if (await MessageBox.Show($"Could not find section: {mapjsr.SubroutineName}\n\n" +
												$"Would you like to select the file it's in?", "Subsection Not Found",
									MessageBoxButton.YesNo) == MessageBoxResult.No) {
								break; // User gives up
							}

							var file2 = await FILEStandard.ShowGenericFileBrowser(
								"Select the MAP file that contains this section");
							if (file2 == null || !file2.Any()) {
								break;
							}

							subMap = FILEStandard.OpenMAPFile(new FileInfo(file2.First())).Result;
							if (subMap == null || !subMap.Scripts.TryGetValue(mapjsr.SubroutineName, out subScript)) {
								// FAILED! Still isn't the right file. Prompt the user for a new file
								subScript = null;
							}
						}
					}

					if (subScript == null) {
						continue; // Could not load the file at all, move on
					}

					this.CurrentState.StateObject.Subsections.Add(subScript);
					int sectionStartTime = currentTime;
					currentTime =
						await this.EnumerateEvents(subScript, eventCanvas, currentTime, autoDereference, layer + 1);
					this.SetupPlayFieldHorizontal(eventCanvas, layer + 1, sectionStartTime, Brushes.DeepSkyBlue,
						currentTime, mapjsr.SubroutineName, "RETURN");
					this.SetupMapLoops(subScript.LevelData, eventCanvas, layer + 1, sectionStartTime);
					this.SelectedScript.MergeSubsection(subScript.LevelData);
				}
			}

			_mapNodeLineBrush = Brushes.Yellow;
			return currentTime;
		}

		private async Task OpenFile(MAPFile file)
		{
			if (file == null) {
				return;
			}
			Console.WriteLine("MAPControl.OpenFile start " + file.OriginalFilePath);

			foreach (var script in file.Scripts.Values)
				if (_tabMap.ContainsKey(script)) {
					return;
				}

			//ASYNC DISABLE
			IsEnabled = false;
			Console.WriteLine("MAPControl.OpenFile having " + file.Scripts.Count + " scripts");

			if (file.Scripts.Count == 0) {
				return;
			}

			if (file.Scripts.Count == 1) {
				await OpenScript(file.Scripts.Values.First());
			} else { // Many scripts here, prompt the user to pick one
				var dialog = new GenericMenuDialog("LEVEL SCRIPTS",
					"The file you selected may have multiple scripts in it.\n" +
					"Select the one you would like to view.",
					file.Scripts.Select(x => $"{x.Key} ({x.Value.LevelData.Events.Count} events)").ToArray());
				if (await dialog.ShowDialog<bool>(Application.Current.MainWindow())) {
					await OpenScript(file.Scripts.Values.ElementAtOrDefault(dialog.Selection));
				}
			}

			IsEnabled = true;
			Console.WriteLine("MAPControl.OpenFile end");
		}

		private async Task OpenScript(MAPScript script)
		{
			if (script == null) {
				return;
			}

			//ASYNC DISABLE
			IsEnabled = false;
			Console.WriteLine("MAPControl.OpenScript start " + script.Header);

			MAP_FINST state = new MAP_FINST();

			if (_tabMap.ContainsKey(script)) {
				state = _tabMap[script];
			}

			if (state.Tab == null) {
				state.Tab = new TabItem() {
					Header = script.Header.LevelName ?? script.Header.LevelMacroName,
					Tag    = script
				};
				MAPTabViewer.Items.Add(state.Tab);
			}

			var tabItem = state.Tab;
			MAPTabViewer.SelectionChanged -= ChangeFile;
			MAPTabViewer.SelectedItem     =  tabItem;
			MAPTabViewer.SelectionChanged += ChangeFile;
			_tabMap.TryAdd(script, state);

			bool autoDereference = script.ReferencedSubSections.Any();
			if (!AppResources.MapImporterAutoDereferenceMode && autoDereference) {
				if (await MessageBox.Show("Automatically include referenced sub-sections of Map files is OFF.\n" +
									$"This level contains {script.ReferencedSubSections.Count} subsections, include them anyway?\n" +
									$"\n(this may take some time)",
						"Auto-Include Sub-Sections?", MessageBoxButton.YesNo) == MessageBoxResult.No) {
					autoDereference = false;
				}
			}

			Panel eventCanvas;

			if (!state.StateObject.Loaded) {
				var dragCanvas = new PanAndZoomCanvas() {
					Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255))
				};
				dragCanvas.LocationChanged       += CanvasMoved;
				state.StateObject.ContentControl =  dragCanvas;
				eventCanvas                      =  state.StateObject.ContentControl;
				int width = await SetupEditor(script, eventCanvas, autoDereference) + 200;
				state.StateObject.LevelWidth = width;
			}

			eventCanvas = state.StateObject.ContentControl;

			CurrentEditorControl.Content = eventCanvas;
			//ASYNC ENABLE
			IsEnabled = true;
			Console.WriteLine("MAPControl.OpenScript end");
		}

		private async void ChangeFile(object sender, SelectionChangedEventArgs e)
		{
			await this.OpenScript((MAPTabViewer.SelectedItem as TabItem).Tag as MAPScript);
		}

		private async Task<int> SetupEditor(MAPScript file, Panel eventCanvas, bool autoDereference)
		{
			int time = 0;
			_expandedX = 0;
			_treeLayer = 0;
			_lastTime  = 0;
			_lastX     = 0;

			time = await EnumerateEvents(file, eventCanvas, time, autoDereference);
			SetupPlayFieldHorizontal(eventCanvas, 0, 0, Brushes.Yellow, time);
			SetupMapLoops(file.LevelData, eventCanvas, 0, 0);

			await SwitchEditorBackground(file.LevelContext);
			return time;
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			BackgroundRender.ResizeViewports((int)Width, (int)Height);
		}

		private async void RefreshEditorButton_Click(object sender, RoutedEventArgs e)
		{
			//await SetupEditor(selectedFile, EventCanvas, true);
		}

		/// <summary>
		/// Changes the editor's background and also the backgrounds of any attached views that this applies
		/// to (like a <see cref="MAP3DControl"/>)
		/// </summary>
		/// <param name="definition"></param>
		/// <returns></returns>
		private async Task SwitchEditorBackground(MAPContextDefinition definition)
		{
			_selectedContext = definition;
			//Update the context of the background viewer
			await BackgroundRender.SetContext(definition);
			BackgroundRender.ResizeViewports((int)Width, (int)Height);
			/* TODO : Set 3D scene viewer background to match the one selected, if opened
			if (MapWindowOpened && definition != null) {
				await _mapWindow.SetContext(definition);
			}// */
		}

		/// <summary>
		/// Calls <see cref="SwitchEditorBackground(MAPContextDefinition?)"/> with the <see cref="_selectedContext"/>
		/// </summary>
		/// <returns></returns>
		private Task SwitchEditorBackground() => SwitchEditorBackground(_selectedContext);

		/// <summary>
		/// This function is used when the user selects a MapNode in the MAPControl.
		/// <para/>Map Nodes can represent many types of information, using the <paramref name="componentSelected"/>
		/// can narrow down what the user actually meant to select to get more info on.
		/// <para/><paramref name="componentSelected"/> being null indicates it's unclear what they meant to select
		/// and the most generic action should be taken
		/// </summary>
		/// <param name="mapEvent"></param>
		/// <param name="componentSelected">Must be of type <see cref="IMAPEventComponent"/></param>
		/// <returns></returns>
		internal async Task<bool> MapNodeSelected(MAPEvent mapEvent, Type componentSelected)
		{
			var typImec = typeof(IMAPEventComponent);
			if (!componentSelected.FindInterfaces(Module.FilterTypeName, typImec.Name).Contains(typImec)) {
				throw new ArgumentException("Selected Component Type is not a IMAPEventComponent");
			}

			//SWITCH BACKGROUND TO THIS
			if (mapEvent is IMAPBGEvent bgEvent && componentSelected == typeof(IMAPBGEvent)) {
				await SwitchEditorBackground(FILEStandard.MAPImport.FindContext(bgEvent.Background));
				return true;
			}

			//SWITCH TO SHAPE VIEWER TO VIEW SHAPE SELECTED
			if (mapEvent is IMAPShapeEvent shapeEvent && componentSelected == typeof(IMAPShapeEvent)) {
				// TODO : Call EDITORStandard.ShapeEditor_ShowShapeByName in MAPControl.MapNodeSelected
				//if (!await EDITORStandard.ShapeEditor_ShowShapeByName(shapeEvent.ShapeName, -1)) {
					MessageBox.Show("Couldn't find any shapes by the name of: " + shapeEvent.ShapeName,
						"Switch Shape");
					return false;
				/*} else {
					return true;
				}// */
			}

			/* TODO : CHECK 3D VIEWER OPENED
			if (MapWindowOpened) { // 3D CONTEXT
				return _mapWindow.CameraTransitionToObject(mapEvent);
			}// */

			//NO 3D VIEWER ATTACHED CONTEXT
			// TODO : Call EDITORStandard.AsmEditor_OpenSymbol in MAPControl.MapNodeSelected
			//await EDITORStandard.AsmEditor_OpenSymbol(mapEvent.Callsite); // open the symbol in the assembly viewer
			return true;
		}

		private void ChronologySlider_Scroll(object sender, Avalonia.Controls.Primitives.ScrollEventArgs scrollEventArgs)
		{
			if (CurrentState != null) {
				var control = (PanAndZoomCanvas)this.CurrentState.StateObject.ContentControl;
				control.LocationChanged -= CanvasMoved;
				double value = CurrentState.StateObject.LevelWidth * ChronologySlider.Value;
				control.SetCanvasLocation(new Point(value, 0));
				control.LocationChanged += CanvasMoved;
			}
		}

		private void CanvasMoved(object sender, Point e)
		{
			var control = (PanAndZoomCanvas)sender;
			ChronologySlider.Scroll -= ChronologySlider_Scroll;
			ChronologySlider.Value = Math.Min(1, Math.Max(0, control.Location.X / CurrentState.StateObject.LevelWidth));
			ChronologySlider.Scroll += ChronologySlider_Scroll;
		}
	}
}
