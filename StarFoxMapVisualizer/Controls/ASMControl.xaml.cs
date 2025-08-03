using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer.Controls
{
	/// <summary>
	/// Interaction logic for ASMControl.xaml
	/// </summary>
	public partial class ASMControl : UserControl
	{
		private const byte MinTextSize = 9;
		private const byte BaseTextSize = 12;
		private const byte MaxTextSize = 96;
		private const double ZoomStepFactor = 1.095445115; // Math.Sqrt(1.2)

		private ASM_FINST<AsmAvalonEditor> current;
		private AsmAvalonEditor EditorScreen => current?.EditorScreen;
		private Dictionary<string, ASM_FINST<AsmAvalonEditor>> fileInstanceMap = new Dictionary<string, ASM_FINST<AsmAvalonEditor>>();

		/// <summary>
		/// The queue of <see cref="OpenFileContents(FileInfo, ASMFile?)"/> calls made while paused
		/// </summary>
		private Queue<FOPENCALL> chewQueue = new Queue<FOPENCALL>();

		/// <summary>
		/// Gets whether this control is paused. See: <see cref="Pause"/>
		/// </summary>
		public bool Paused { get; private set; }

		public ASMControl()
		{
			InitializeComponent();
			FileBrowserTabView.Items.Clear();
			FileBrowserTabView.SelectionChanged += FileBrowserTabView_SelectionChanged;
		}

		private void FileBrowserTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = TabItemTagAt(FileBrowserTabView.SelectedIndex);
			if (tag != null) {
				current = tag;
				DisplayEditorTab(tag, null);
			}
		}

		private void DisplayEditorTab(ASM_FINST<AsmAvalonEditor> tag, ASMChunk chunk)
		{
			FilePathBlock.Text = tag.OpenFile.FullName;
			current.EditorScreen.Focus();
			if (chunk != null) {
				current.EditorScreen.ScrollToSymbol(chunk);
			}
			_ = Dispatcher.InvokeAsync(current.EditorScreen.Focus, DispatcherPriority.ApplicationIdle);
		}

		/// <summary>
		/// Any calls made to <see cref="OpenFileContents(FileInfo, ASMFile?)"/> are queued until this control is <see cref="Unpause"/>'d
		/// </summary>
		public void Pause()
		{
			Paused = true;
			IsEnabled = false;
		}
		/// <summary>
		/// Unpauses the control and runs all calls to <see cref="OpenFileContents(FileInfo, ASMFile?)"/> asyncronously
		/// </summary>
		/// <returns></returns>
		public async Task Unpause()
		{
			Paused = false;
			IsEnabled = true;
			while (TryDequeue(chewQueue, out var call))
				await DoFileOpenTaskAsync(call);
		}

		private static bool TryDequeue<T>(/*this*/ Queue<T> self, out T result)
		{
			if ((self != null) && (self.Count > 0)) {
				try {
					result = self.Dequeue();
					return true;
				}
				catch (Exception) {
					result = default;
					return false;
				}
			}
			else {
				result = default;
				return false;
			}
		}

		private ASM_FINST<AsmAvalonEditor> TabItemTagAt(int index)
		{
			if (index >= 0) {
				// WTF M$, using weakly typed collections when you already have GENERICS
				var selectedTab = FileBrowserTabView.Items[index] as TabItem;
				return selectedTab?.Tag as ASM_FINST<AsmAvalonEditor>;
			}
			else {
				return null;
			}
		}

		private class FOPENCALL
		{
			public FileInfo FileSelected;
			public ASMFile FileData;
			/// <summary>
			/// The symbol to jump to after opening, if applicable
			/// </summary>
			public ASMChunk Chunk;
		}
		private async Task DoFileOpenTaskAsync(FOPENCALL call)
		{
			void OpenTab(ASM_FINST<AsmAvalonEditor> inst)
			{
				FileBrowserTabView.SelectedItem = inst.Tab; // select the tab
				current = inst;
				DisplayEditorTab(inst, call.Chunk);
			}
			if (fileInstanceMap.TryGetValue(call.FileSelected.FullName, out var finst)) {
				OpenTab(finst);	// select the tab
				return;
			}
			foreach (var fileInstance in fileInstanceMap.Values) {
				if (call.FileSelected.FullName == fileInstance.OpenFile.FullName) {	// FILE Opened?

					OpenTab(fileInstance);	// select the tab
					return;
				}
			}
			var tab = new TabItem()
			{
				Header = call.FileSelected.Name,
				ToolTip = call.FileSelected.FullName
			};
			tab.MouseDoubleClick += delegate(object sender, MouseButtonEventArgs args)
			{
				if (!(args.OriginalSource is ICSharpCode.AvalonEdit.Rendering.TextView)) {
					int selectedIndex = FileBrowserTabView.SelectedIndex;
					if (selectedIndex >= 0) {
						var tagged = TabItemTagAt(selectedIndex);
						if (tagged != null) {
							fileInstanceMap.Remove(tagged.OpenFile.FullName);
						}
						FileBrowserTabView.Items.RemoveAt(selectedIndex);
					}
					// more tabs, switch to the next one to the left
					if (FileBrowserTabView.Items.Count <= 1) {
						selectedIndex = -1;
					} else if (selectedIndex > 0) {
						selectedIndex--;
					}
					if (selectedIndex > -1) {
						FileBrowserTabView.SelectedIndex = selectedIndex;
						var tagged = TabItemTagAt(selectedIndex);
						if (tagged != null) {
							current = tagged;
							DisplayEditorTab(tagged, null);
						}
					}
				}
			};
			var instance = new ASM_FINST<AsmAvalonEditor>()
			{
				OpenFile = call.FileSelected,
				SymbolMap = new Dictionary<ASMChunk, Run>(),
				Tab = tab,
				FileImportData = call.FileData
			};
			tab.Tag = instance;
			var newEditZone = new AsmAvalonEditor(this, instance)
			{
				FontSize = BaseTextSize
			};
			instance.StateObject = newEditZone;
			tab.Content = newEditZone;

			fileInstanceMap.Add(call.FileSelected.FullName, instance);
			FileBrowserTabView.Items.Add(tab);
			OpenTab(instance);
			await ParseAsync(call.FileSelected);
		}

		public async Task OpenFileContents(FileInfo FileSelected, ASMFile FileData = default, ASMChunk Symbol = default)
		{
			var call = new FOPENCALL()
			{
				FileSelected = FileSelected,
				FileData = FileData,
				Chunk = Symbol,
			};
			if (Paused) {
				chewQueue.Enqueue(call);
				return;
			}
			await DoFileOpenTaskAsync(call);
		}

		public Task OpenSymbol(ASMChunk chunk) => OpenFileContents(new FileInfo(chunk.OriginalFileName), null, chunk);

		private DispatcherOperation ParseAsync(FileInfo File)
		{
			return Dispatcher.InvokeAsync(delegate
			{
				EditorScreen.InvalidateFileContents();
			});
		}

		private void ButtonZoomRestore_Click(object sender, RoutedEventArgs e)
		{
			EditorScreen.FontSize = BaseTextSize;
		}

		private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
		{
			// The compiler will turn the division of the constant 
			// to a multiplication of its reciprocal.
			EditorScreen.FontSize = Math.Max(MinTextSize, EditorScreen.FontSize * (1.0 / ZoomStepFactor));
		}

		private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
		{
			EditorScreen.FontSize = Math.Min(MaxTextSize, EditorScreen.FontSize * ZoomStepFactor);
		}
	}
}
