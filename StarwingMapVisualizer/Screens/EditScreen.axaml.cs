using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MAP;
using StarFox.Interop.MSG;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer.Screens
{
	public partial class EditScreen : UserControl
	{
		/// <summary>
		/// See: <see cref="EDITORStandard.CurrentEditorScreen"/>
		/// </summary>
		internal static EditScreen Current => EDITORStandard.CurrentEditorScreen;

		public enum ViewMode
		{
			NONE,
			ASM,
			MAP,
			OBJ,
			GFX,
			MSG,
			BRR
		}

		public ViewMode CurrentMode { get; set; }

		public EditScreen()
		{
			InitializeComponent();

			SolutionExplorerView.Items.Clear();
			MacroExplorerView.Items.Clear();

			Loaded += OnLoad;
		}

		private async void OnLoad(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.NONE;
			await HandleViewModes();
			await UpdateInterface();
			EDITORStandard.HideLoadingWindow();
		}

		/// <summary>
		/// Will Load (or Reload) the current project
		/// <para></para>
		/// </summary>
		/// <param name="flush"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException"></exception>
		internal async Task ImportCodeProject(bool flush = false)
		{
			var currentProject = AppResources.ImportedProject;
			if (currentProject == null) {
				throw new InvalidDataException("No project loaded.");
			}

			//Show welcome wagon if not shown once to the user yet this session
			bool changesMade = await EDITORStandard.WelcomeWagon();

			if (flush || changesMade) {
				await currentProject.EnumerateAsync();
			}

			var expandedHeaders = new List<string>();

			void CheckNode(in TreeViewItem node)
			{
				if (node.IsExpanded && node.Tag != null && node.Tag is SFCodeProjectNode sfNode) {
					expandedHeaders.Add(sfNode.FilePath);
				}

				foreach (var treeNode in node.Items.OfType<TreeViewItem>()) {
					CheckNode(in treeNode);
				}
			}

			foreach (var treeNode in SolutionExplorerView.Items.OfType<TreeViewItem>()) {
				CheckNode(in treeNode);
			}

			var selectedItemHeader = (SolutionExplorerView.SelectedItem as TreeViewItem)?.Tag as SFCodeProjectNode;
			SolutionExplorerView.Items.Clear();

			void CreateClosableContextMenu(SFCodeProjectNode fileNode, in ContextMenu contextMenu,
			string message = "Close File")
			{
				//CLOSABLE FILE ITEM
				var importItem = new MenuItem() {
					Header = message
				};
				importItem.Click += delegate {
					//DO INCLUDE
					var result = AppResources.ImportedProject.CloseFile(fileNode.FilePath);
					if (!result) {
						MessageBox.Show("That file could not be closed at this time.", "File Close Error");
					}

					_ = UpdateInterface();
				};
				contextMenu.Items.Add(importItem);
			}

			void CreateINCContextMenu(SFCodeProjectNode fileNode, in ContextMenu contextMenu,
			string message = "Include File")
			{
				//INCLUDE FILE ITEM
				var importItem = new MenuItem() {
					Header = message
				};
				importItem.Click += async delegate {
					//DO INCLUDE
					var result =
						await FILEStandard.IncludeFile<object>(new FileInfo(fileNode.FilePath),
							SFFileType.ASMFileTypes.ASM) != null;
					if (!result) {
						MessageBox.Show("That file could not be imported at this time.", "File Include Error");
					}

					await UpdateInterface();
				};
				contextMenu.Items.Add(importItem);
			}

			void CreateIncludeDirectoryAsBRRContextMenu(SFCodeProjectNode fileNode, in ContextMenu contextMenu,
			string message = "Open All *.BRR Files")
			{
				//INCLUDE DIRECTORY ITEM
				var importItem = new MenuItem() {
					Header = message
				};
				importItem.Click += async delegate {
					EDITORStandard.ShowLoadingWindow();
					foreach (var brrNode in fileNode.ChildNodes.Where(x =>
								 x.RecognizedFileType is SFCodeProjectFileTypes.BRR)) {
						// TODO : Import AUDIOStandard
						//await AUDIOStandard.OpenBRR(new FileInfo(brrNode.FilePath), false, false, true);
					}

					await UpdateInterface();
					CurrentMode = ViewMode.BRR;
					await HandleViewModes();
					EDITORStandard.HideLoadingWindow();
				};
				contextMenu.Items.Add(importItem);
			}

			void CreateCOLTABContextMenu(SFCodeProjectNode fileNode, in ContextMenu contextMenu,
			string message = "Include File as Color Table")
			{
				//INCLUDE FILE ITEM
				var importItem = new MenuItem() {
					Header = message
				};
				importItem.Click += async delegate {
					//DO INCLUDE
					var result = await FILEStandard.TryIncludeColorTable(new FileInfo(fileNode.FilePath));
					if (!result) {
						MessageBox.Show("That file could not be imported at this time.", "File Include Error");
					}

					await UpdateInterface();
				};
				contextMenu.Items.Add(importItem);
			}

			void CreateExploreContextMenu(SFCodeProjectNode fileNode, in ContextMenu contextMenu,
			string message = "Show in File Explorer")
			{
				//FILE EXPLORER CONTEXT MENU
				var importItem = new MenuItem() {
					Header = message
				};
				importItem.Click += delegate { OpenExternal.Folder(fileNode.FilePath); };
				contextMenu.Items.Add(importItem);
			}

			async Task<TreeViewItem> AddProjectNode(SFCodeProjectNode Node)
			{
				var node = new TreeViewItem() {
					IsExpanded = true,
					Tag        = Node
				};
				ToolTip.SetTip(node, Node.FilePath);
				node.Classes.Add("ProjectTreeStyle");

				foreach (var child in Node.ChildNodes) {
					if (child.Type == SFCodeProjectNodeTypes.Directory) {
						await AddDirectory(node, child);
					} else if (child.Type == SFCodeProjectNodeTypes.File) {
						await AddFile(node, child);
					}
				}

				return node;
			}

			async Task AddDirectory(TreeViewItem parent, SFCodeProjectNode dirNode)
			{
				var menu = new ContextMenu();
				var thisTreeNode = new TreeViewItem() {
					Header      = Path.GetFileName(dirNode.FilePath),
					Tag         = dirNode,
					ContextMenu = menu,
					Margin      = new Thickness(0)
				};
				ToolTip.SetTip(thisTreeNode, String.Format("{0} subdirectories and {1} files (not recursive)",
					dirNode.ChildNodes.Count(x => x.Type == SFCodeProjectNodeTypes.Directory),
					dirNode.ChildNodes.Count(x => x.Type == SFCodeProjectNodeTypes.File)));
				thisTreeNode.Classes.Add("FolderTreeStyle");
				CreateIncludeDirectoryAsBRRContextMenu(dirNode, menu);
				CreateExploreContextMenu(dirNode, menu);
				foreach (var child in dirNode.ChildNodes) {
					if (child.Type == SFCodeProjectNodeTypes.Directory) {
						await AddDirectory(thisTreeNode, child);
					} else if (child.Type == SFCodeProjectNodeTypes.File) {
						await AddFile(thisTreeNode, child);
					}
				}

				if (expandedHeaders.Contains(dirNode.FilePath)) { // was expanded
					thisTreeNode.IsExpanded = true;
				}

				parent.Items.Add(thisTreeNode);
			}

			async Task AddFile(TreeViewItem Parent, SFCodeProjectNode fileNode)
			{
				var fileInfo    = new FileInfo(fileNode.FilePath);
				var contextMenu = new ContextMenu();
				var item = new TreeViewItem() {
					Header      = fileInfo.Name,
					Tag         = fileNode,
					ContextMenu = contextMenu,
					Margin      = new Thickness(0)
				};
				ToolTip.SetTip(item, $"{fileInfo.Length:n0} bytes");
				CreateExploreContextMenu(fileNode, contextMenu);
				switch (fileNode.RecognizedFileType) {
					case SFCodeProjectFileTypes.Palette:
					retry:
						item.Classes.Add("PaletteTreeStyle");
						if (!AppResources.IsFileIncluded(fileInfo)) {
							if (await FILEStandard.IncludeFile<object>(fileInfo) != null) {
								goto retry;
							}

							CreateINCContextMenu(fileNode, in contextMenu);
							item.Foreground = Brushes.White; // Indicate with white that it isn't included yet
						}

						break;
					case SFCodeProjectFileTypes.SCR:
						item.Classes.Add("ScreenTreeStyle");
						if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName)) {
							CreateClosableContextMenu(fileNode, in contextMenu);
						}

						break;
					case SFCodeProjectFileTypes.CGX:
						item.Classes.Add("SpriteTreeStyle");
						if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName)) {
							CreateClosableContextMenu(fileNode, in contextMenu);
						}

						break;
					case SFCodeProjectFileTypes.Include:
						if (!AppResources.IsFileIncluded(fileInfo)) {
							item.Classes.Add("FileTreeStyle");
							CreateINCContextMenu(fileNode, in contextMenu);
						} else {
							item.Classes.Add("FileImportTreeStyle");
						}

						break;
					default:
						// allow other files to be included under certain circumstances
						var include = AppResources.ImportedProject?.GetInclude(fileInfo);
						if (include != null) {
							item.Classes.Add(include is COLTABFile ? "ColorTableTreeStyle" : "FileImportTreeStyle");
						} else {
							CreateINCContextMenu(fileNode, in contextMenu);
							CreateCOLTABContextMenu(fileNode, in contextMenu);
							item.Classes.Add("FileTreeStyle");
						}

						break;
				}

				item.PointerPressed += async delegate {
					try {
						await FileSelected(fileInfo);
					} catch (Exception ex) {
						AppResources.ShowCrash(ex, false, "The plug-in selected could not complete that task.");
					} finally {
						EDITORStandard.HideLoadingWindow();
					}
				};
				if (selectedItemHeader != null && selectedItemHeader.FilePath == fileNode.FilePath) { // was selected
					item.BringIntoView();
				}

				Parent.Items.Add(item);
			}

			SolutionExplorerView.Items.Add(await AddProjectNode(currentProject.ParentNode));
		}

		/// <summary>
		/// Changes the current Editor View Mode to the one provided
		/// </summary>
		/// <param name="view"></param>
		/// <returns></returns>
		public async Task SwitchView(ViewMode view)
		{
			if (view == CurrentMode) {
				return;
			}

			CurrentMode = view;
			await HandleViewModes();
		}

		/// <summary>
		/// Should be called after changing the <see cref="CurrentMode"/> property
		/// <para>Will update the interface to match the new Mode</para>
		/// </summary>
		/// <returns></returns>
		public Task HandleViewModes() => Dispatcher.UIThread.InvokeAsync(async delegate {
			Console.WriteLine("EditScreen.HandleViewModes start in ui thread");
			//FIRST LOAD
			MainViewerBorder.IsVisible = true;

			//UNSUBSCRIBE FROM ALL BUTTONS FIRST
			ViewASMButton.IsCheckedChanged -= ViewASMButton_Checked;
			ViewMapButton.IsCheckedChanged -= ViewMapButton_Checked;
			ViewBSTButton.IsCheckedChanged -= ViewBSTButton_Checked;
			ViewGFXButton.IsCheckedChanged -= ViewGFXButton_Checked;
			ViewMSGButton.IsCheckedChanged -= ViewMSGButton_Checked;
			ViewBRRButton.IsCheckedChanged -= ViewBRRButton_Checked;

			//UNCHECK EM ALL
			ViewGFXButton.IsChecked = false;
			ViewASMButton.IsChecked = false;
			ViewMapButton.IsChecked = false;
			ViewBSTButton.IsChecked = false;
			ViewMSGButton.IsChecked = false;
			ViewBRRButton.IsChecked = false;

			Console.WriteLine("EditScreen.HandleViewModes before pausing viewers");
			//VIEW MODES ENABLED
			ViewModeHost.IsVisible = true;
			// TODO : Pause viewers in HandleViewModes
			MAPViewer.Pause();
			//ASMViewer.Pause();
			//OBJViewer.Pause();
			Console.WriteLine("EditScreen.HandleViewModes after pausing viewers, CurrentMode is " + CurrentMode);

			switch (CurrentMode) {
				default:
				case ViewMode.NONE:
					MainViewerBorder.IsVisible = false;
					break;
				case ViewMode.ASM:
					// TODO: Unpause assembly viewer
					//await ASMViewer.Unpause();
					ViewModeHost.SelectedItem = ASMTab;
					ViewASMButton.IsChecked   = true;
					TitleBlock.Text           = "Assembly Viewer";
					break;
				case ViewMode.MAP:
					MAPViewer.Unpause();
					ViewModeHost.SelectedItem = MAPTab;
					ViewMapButton.IsChecked   = true;
					TitleBlock.Text           = "Map Event Node Viewer";
					break;
				case ViewMode.OBJ:
					// TODO: Unpause 3D shape viewer
					//OBJViewer.Unpause();
					ViewModeHost.SelectedItem = OBJTab;
					ViewBSTButton.IsChecked   = true;
					TitleBlock.Text           = "Shape Viewer";
					break;
				case ViewMode.GFX:
					// TODO : Refresh 2D image viewer
					//GFXViewer.RefreshFiles();
					ViewModeHost.SelectedItem = GFXTab;
					ViewGFXButton.IsChecked   = true;
					TitleBlock.Text           = "Graphics Viewer";
					break;
				case ViewMode.MSG:
					await MSGViewer.RefreshFiles();
					ViewModeHost.SelectedItem = MSGTab;
					ViewMSGButton.IsChecked   = true;
					TitleBlock.Text           = "Message Viewer";
					break;
				case ViewMode.BRR:
					// TODO : Refresh sound effects viewer
					//BRRViewer.RefreshFiles();
					ViewModeHost.SelectedItem = BRRTab;
					ViewBRRButton.IsChecked   = true;
					TitleBlock.Text           = "SFX Viewer";
					break;
			}

			ViewASMButton.IsCheckedChanged += ViewASMButton_Checked;
			ViewMapButton.IsCheckedChanged += ViewMapButton_Checked;
			ViewBSTButton.IsCheckedChanged += ViewBSTButton_Checked;
			ViewGFXButton.IsCheckedChanged += ViewGFXButton_Checked;
			ViewMSGButton.IsCheckedChanged += ViewMSGButton_Checked;
			ViewBRRButton.IsCheckedChanged += ViewBRRButton_Checked;
			Console.WriteLine("EditScreen.HandleViewModes end in ui thread");
		});

		/// <summary>
		/// Will handle known file types and return true if handled.
		/// <para>Returns false if not handled. Returns default if the user cancels.</para>
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private async Task<bool?> HandleKnownFileTypes(FileInfo file)
		{
			var enuSFT = file.GetSFFileType();              // YEAH?
			if (enuSFT == SFCodeProjectFileTypes.Palette) { // HANDLE PALETTE
				await FILEStandard.OpenPalette(file);
				EDITORStandard.HideLoadingWindow();
				await this.UpdateInterface();
				return true;
			} else if (enuSFT == SFCodeProjectFileTypes.BRR) {
				// TODO : Open then select BRR file
				//await AUDIOStandard.OpenBRR(file);
				await this.UpdateInterface();
				this.CurrentMode = ViewMode.BRR;
				await this.HandleViewModes();
				EDITORStandard.HideLoadingWindow();
				//BRRViewer.SelectFile(file.FullName);
				return true;
			} else if (enuSFT == SFCodeProjectFileTypes.SPC) {
				// TODO : open SPC file properties
				//await AUDIOStandard.OpenSPCProperties(file);
				EDITORStandard.HideLoadingWindow();
				return true;
			} else if (enuSFT == SFCodeProjectFileTypes.BINFile) { // EXTRACT BIN
				// DOUBT AS TO FILE TYPE
				//CREATE THE MENU WINDOW
				/* TODO : Import BINImportMenu
				var importMenu = new BINImportMenu();
				if (!importMenu.ShowDialog(Application.Current.MainWindow) ?? true) {
					return null; // USER CANCEL
				}

				var selectFileType = importMenu.FileType;
				if (selectFileType == SFFileType.BINFileTypes.COMPRESSED_CGX) {
					await SFGFXInterface.TranslateDATFile(file.FullName);
					await this.UpdateInterface(true); // Files changed!
				} else if (selectFileType == SFFileType.BINFileTypes.BRR) {
					await AUDIOStandard.OpenBRR(file);
					await this.UpdateInterface();
					this.CurrentMode = ViewMode.BRR;
					await this.HandleViewModes();
				} else if (selectFileType == SFFileType.BINFileTypes.SPC) {
					if (await AUDIOStandard.ConvertBINToSPC(file)) {
						await this.UpdateInterface(true);
					}
				}

				EDITORStandard.HideLoadingWindow();
				return true;
				// */
				return null;
			} else if (enuSFT == SFCodeProjectFileTypes.CCR) { // EXTRACT COMPRESSED GRAPHICS
				await GFXStandard.ExtractCCR(file);
				EDITORStandard.HideLoadingWindow();
				await this.UpdateInterface(true); // Files changed!
				return true;
			} else if (enuSFT == SFCodeProjectFileTypes.PCR) { // EXTRACT COMPRESSED GRAPHICS
				await SFGFXInterface.TranslateCompressedPCR(file.FullName);
				EDITORStandard.HideLoadingWindow();
				await this.UpdateInterface(true); // Files changed!
				return true;
			} else if (file.GetSFFileType() == SFCodeProjectFileTypes.SCR) { // screens
				//OPEN THE SCR FILE
				GFXStandard.OpenSCR(file);
				EDITORStandard.HideLoadingWindow();
				await this.UpdateInterface();
				this.CurrentMode = ViewMode.GFX;
				await this.HandleViewModes();
				return true;
			} else if (file.GetSFFileType() == SFCodeProjectFileTypes.CGX) { // graphics
				//OPEN THE CGX FILE
				await GFXStandard.OpenCGX(file);
				EDITORStandard.HideLoadingWindow();
				await this.UpdateInterface();
				this.CurrentMode = ViewMode.GFX;
				await this.HandleViewModes();
				return true;
			}

			return false;
		}

		/// <summary>
		/// A file has been selected in the GUI
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private async Task FileSelected(FileInfo file)
		{
			EDITORStandard.ShowLoadingWindow();
			//CHECK IF IT'S A KNOWN FILE
			var result = await HandleKnownFileTypes(file);
			if (!result.HasValue || result.Value) {
				return; // Handled or User Cancelled
			}

			//SWITCH TO ASM VIEWER IF WE HAVEN'T ALREADY
			CurrentMode = ViewMode.ASM;
			//HANDLE VIEW MODES -- PAUSE / ENABLE VIEW MODE CONTROLS
			await HandleViewModes();
			//DO FILE PARSE NOW
			var  asmfile  = await FILEStandard.OpenASMFile(file, file.IsPlainAssemblyOnly());
			bool isMap    = asmfile is MAPFile;
			bool isObj    = asmfile is BSPFile;
			bool isMsg    = asmfile is MSGFile;
			bool isDEFSPR = asmfile is MSpritesDefinitionFile;
			if (asmfile == null) {
				EDITORStandard.HideLoadingWindow();
				return;
			}

			// FILE INCLUDE ROUTINE
			if (file.GetSFFileType() is SFCodeProjectFileTypes.Include && !isMsg) {
				// INC files should be included automatically -- generally.
				FILEStandard.IncludeFile(asmfile);
			}

			// GET DEFAULT ACTION
			Console.WriteLine("EditScreen.FileSelected get default action");
			if (isMap) {
				// IF THIS IS A MAP -- SWITCH VIEW, INCUR UPDATE. THE MAP VIEW WILL SEE THE NEWLY ADDED FILE
				CurrentMode = ViewMode.MAP;
				await HandleViewModes();
			} else if (isObj) {
				// IF THIS IS AN OBJ -- SWITCH VIEW, INCUR UPDATE. THE OBJ VIEW WILL SEE THE NEWLY ADDED FILE
				CurrentMode = ViewMode.OBJ;
				await HandleViewModes();
			} else if (isMsg) { // COMMUNICATIONS VIEWER
				CurrentMode = ViewMode.MSG;
				await HandleViewModes();
			} else if (isDEFSPR) { // 3D MSPRITES VIEWER
				/* TODO : Import MSpritesViewer in FileSelected
				MSpritesViewer viewer = new MSpritesViewer(asmfile as MSpritesDefinitionFile);
				viewer.Show();
				// */
			} else {
				//ENQUEUE THIS FILE TO BE OPENED BY THE ASM VIEWER
				// TODO : Import ASMViewer
				//await ASMViewer.OpenFileContents(file, asmfile); // tell the ASMControl to look at the new file
			}

			Console.WriteLine("EditScreen.FileSelected update interface");
			await UpdateInterface();
			Console.WriteLine("EditScreen.FileSelected hide loaing window");
			EDITORStandard.HideLoadingWindow();
			Console.WriteLine("EditScreen.FileSelected set macrofile combo box");
			MacroFileCombo.SelectedValue = Path.GetFileNameWithoutExtension(file.Name);
		}

		/// <summary>
		/// Refreshes the Workspace Explorer, Macros and Open Files for some Editors
		/// </summary>
		/// <param name="flushFiles"></param>
		private async Task UpdateInterface(bool flushFiles = false)
		{
			//update explorer
			await ImportCodeProject(flushFiles);
			//UPDATE INCLUDES
			MacroFileCombo.ItemsSource =
				AppResources.Includes.Select(x => Path.GetFileNameWithoutExtension(x.OriginalFilePath));
			//VIEW MODE
			// TODO : Invalidate file lists in GFXViewer
			if (CurrentMode == ViewMode.MAP) {
				await MAPViewer.InvalidateFiles();
			} /*else if (CurrentMode == ViewMode.GFX) {
				GFXViewer.RefreshFiles();
			}// */
		}

		/// <summary>
		/// Shows what Macros are defined in the current <see cref="ASMFile"/>
		/// </summary>
		/// <param name="file"></param>
		private void ShowMacrosForFile(ASMFile file)
		{
			void AddSymbol<T>(T symbol) where T : ASMChunk, IASMNamedSymbol
			{
				var wpfTooltip = new ToolTip() {
					Foreground    = Brushes.White,
					Content       = symbol.ToString() //.ToTextBlock()
				};
				if (symbol is ASMMacro) {
					wpfTooltip.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x4F, 0x69));
				} else if (symbol is ASMConstant) {
					wpfTooltip.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x00, 0x7F));
				}

				var item = new ListBoxItem() {
					Content = symbol.Name,
					Tag     = symbol
				};
				ToolTip.SetTip(item, wpfTooltip);

				MacroExplorerView.Items.Add(item);
			}

			MacroExplorerView.Items.Clear();
			if (MacroFilterRadio.IsChecked ?? false) {
				MacroExplorerView.Foreground = this.FindResource("MacroColor") as SolidColorBrush;
				var macros = file.Chunks.OfType<ASMMacro>(); // filter all chunks by macros only
				foreach (var macro in macros) {
					AddSymbol(macro);
				}
			} else if (DefineFilterRadio.IsChecked ?? false) {
				MacroExplorerView.Foreground = this.FindResource("DefineColor") as SolidColorBrush;
				var defines = file.Constants; // constants are kept separate
				foreach (var define in defines) {
					if (define != null) {
						AddSymbol(define);
					}
				}
			}
		}

		/// <summary>
		/// Raised when the User changes the file they are viewing Macros on.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FilterFileChanged(object sender, SelectionChangedEventArgs e)
		{
			int newSelection = MacroFileCombo.SelectedIndex;
			if (newSelection < 0) {
				return;
			}

			var file = AppResources.Includes.ElementAtOrDefault(newSelection);
			if (file == null) {
				return;
			}

			ShowMacrosForFile(file);
		}

		/// <summary>
		/// Raised when the user selects a Macro to view in the ASMViewer
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void MacroExplorerView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MacroExplorerView.SelectedItem == null) {
				return;
			}

			if (((ListBoxItem)MacroExplorerView.SelectedItem).Tag is ASMChunk chunk) {
				CurrentMode = ViewMode.ASM;
				await HandleViewModes();
				// TODO : Import ASMViewer for MacroExplorerView_SelectionChanged
				//await ASMViewer.OpenSymbol(chunk);
			}
		}

		/// <summary>
		/// Raised when the user changes the current file viewing Macros on.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FilterChanged(object sender, RoutedEventArgs e)
		{
			FilterFileChanged(sender, null);
		}

		/// <summary>
		/// MapViewer Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewMapButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.MAP;
			HandleViewModes();
			UpdateInterface();
		}

		/// <summary>
		/// ASM Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewASMButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.ASM;
			HandleViewModes();
			UpdateInterface();
		}

		/// <summary>
		/// Model Viewer Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewBSTButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.OBJ;
			HandleViewModes();
			UpdateInterface();
		}

		/// <summary>
		/// Graphics Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewGFXButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.GFX;
			HandleViewModes();
			UpdateInterface();
		}

		/// <summary>
		/// Messages Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewMSGButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.MSG;
			HandleViewModes();
			UpdateInterface();
		}

		/// <summary>
		/// Sound (Samples) Button Clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewBRRButton_Checked(object sender, RoutedEventArgs e)
		{
			CurrentMode = ViewMode.BRR;
			HandleViewModes();
			UpdateInterface();
		}

		private async Task SFOptimRefreshBase(SFOptimizerTypeSpecifiers Type, string Noun)
		{
			_ = await EDITORStandard.Editor_RefreshMap(Type);
			await UpdateInterface(true); // files updated!
		}

		#region XAML menu bar
		/// <summary>
		/// Prompts the user to export all 3D models and will export them
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ExportAll3DButton_Click(object sender, RoutedEventArgs e) =>
			await EDITORStandard.Editor_ExportAll3DShapes(".sfshape");

		private async void ExportAll3DObjButton_Click(object sender, RoutedEventArgs e) =>
			await EDITORStandard.Editor_ExportAll3DShapes(".obj");

		/// <summary>
		/// Refreshes the SHAPESMap SFOptimizer directory with the latest 3D model list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <exception cref="FileNotFoundException"></exception>
		private async void SHAPEMapRefreshButton_Click(object sender, RoutedEventArgs e) =>
			await SFOptimRefreshBase(SFOptimizerTypeSpecifiers.Shapes, "ShapesMap");

		/// <summary>
		/// Refreshes the STAGESMAP SFOptimizer directory with the latest level list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <exception cref="FileNotFoundException"></exception>
		private async void STAGEMapRefreshButton_Click(object sender, RoutedEventArgs e) =>
			await SFOptimRefreshBase(SFOptimizerTypeSpecifiers.Maps, "StagesMap");

		/// <summary>
		/// Opens the Level Background viewer dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BGSASMViewerButton_Click(object sender, RoutedEventArgs e)
		{
			var file = FILEStandard.MAPImport?.LoadedContextDefinitions;
			if (file == null) {
				MessageBox.Show(
					"Level contexts have not been loaded yet. Open a level file to have this information populated.");
				return;
			}

			/* TODO : Import LevelContextViewer for BGSASMViewerButton_Click
			LevelContextViewer viewer = new LevelContextViewer(file) {
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			viewer.Show(Application.Current.MainWindow);
			// */
		}

		private void ExitItem_Click(object sender, RoutedEventArgs e)
		{
			var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			desktop?.Shutdown();
		}

		private async void OpenItem_Click(object sender, RoutedEventArgs e)
		{
			var file = await FILEStandard.ShowGenericFileBrowser("Select a File to View");
			if (file == null || file.Length == 0) {
				return;
			}

			await FileSelected(new FileInfo(file.First()));
		}

		private void CloseProjectMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//Delete old project
			AppResources.ImportedProject = null;
			//switch to landing screen
			((MainWindow)Application.Current.MainWindow()).Content = new LandingScreen();
		}

		private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var settings = new SettingsDialog();
			settings.Show();
		}

		/// <summary>
		/// Fired when the Go... item is opened (this loads Maps, stages, etc.)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoItem_Load(object sender, RoutedEventArgs e)
		{
			GoItem.Items.Clear();
			foreach (var map in AppResources.ImportedProject.Optimizers) {
				if (map.OptimizerData == null) {
					continue;
				}

				MenuItem item = new MenuItem() {
					Header = Enum.GetName(typeof(SFOptimizerTypeSpecifiers), map.OptimizerData.TypeSpecifier),
				};
				foreach (var mapItem in map.OptimizerData.ObjectMap.OrderBy(x => x.Key)) {
					var subItem = new MenuItem() {
						Header = mapItem.Key
					};
					subItem.Click += async delegate {
						var name = mapItem.Key;
						try {
							EDITORStandard.ShowLoadingWindow();
							await EDITORStandard.InvokeOptimizerMapItem(map.OptimizerData.TypeSpecifier, name);
						} catch (Exception ex) {
							AppResources.ShowCrash(ex, false,
								$"Couldn't open this {map.OptimizerData.TypeSpecifier} item.");
						} finally {
							EDITORStandard.HideLoadingWindow();
						}
					};
					item.Items.Add(subItem);
				}

				GoItem.Items.Add(item);
			}

			GoItem.SubmenuOpened -= GoItem_Load;
		}

		/// <summary>
		/// Fired when the Level Select Menu item is selected
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LevelSelectItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO : Import LevelSelectWindow
			//var wnd = new LevelSelectWindow();
			//wnd.Show();
		}

		private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
		{
			new AboutBox().ShowDialog(Application.Current.MainWindow());
		}

		private void OpenProjectFolderItem_Click(object sender, RoutedEventArgs e)
		{
			OpenExternal.Folder(AppResources.ImportedProject.WorkspaceDirectory.FullName, true);
		}

		private async void ConvertSfscreenItem_OnClick(object sender, RoutedEventArgs e)
		{
			await GFXStandard.ConvertFromSfscreen();
		}
		#endregion
	}
}
