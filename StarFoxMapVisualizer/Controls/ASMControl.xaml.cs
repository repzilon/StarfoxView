using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private const double BASE_TEXT_SIZE = 12;

        private ASM_FINST current;
        private ASMCodeEditor EditorScreen => current?.EditorScreen;
        private Dictionary<string, ASM_FINST> fileInstanceMap = new Dictionary<string, ASM_FINST>();

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

		private void DisplayEditorTab(ASM_FINST tag, ASMChunk chunk)
		{
			FilePathBlock.Text = tag.OpenFile.FullName;
			current.EditorScreen.Focus();
			if (chunk != null) {
				current.EditorScreen.JumpToSymbol(chunk);
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
                await doFileOpenTaskAsync(call);
        }

        private static bool TryDequeue<T>(/*this*/ Queue<T> self, out T result)
        {
            if ((self != null) && (self.Count > 0)) {
                try {
                    result = self.Dequeue();
                    return true;
                } catch (Exception) {
					result = default;
					return false;
				}
            } else {
                result = default;
                return false;
            }
        }

		private ASM_FINST TabItemTagAt(int index)
		{
			if (index >= 0) {
				// WTF M$, using weakly typed collections when you already have GENERICS
				var selectedTab = FileBrowserTabView.Items[index] as TabItem;
				return selectedTab?.Tag as ASM_FINST;
			} else {
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
            public ASMChunk chunk;
        }
        private async Task doFileOpenTaskAsync(FOPENCALL Call)
        {
            void OpenTab(ASM_FINST inst)
            {
                FileBrowserTabView.SelectedItem = inst.Tab; // select the tab
                current = inst;
                DisplayEditorTab(inst, Call.chunk);
			}
            if (fileInstanceMap.TryGetValue(Call.FileSelected.FullName, out var finst))
            {
                OpenTab(finst);// select the tab
                return;
            }
            foreach (var fileInstance in fileInstanceMap.Values)
            {
                if (Call.FileSelected.FullName == fileInstance.OpenFile.FullName) // FILE Opened?
                {
                    OpenTab(fileInstance);// select the tab
                    return;
                }
            }
            var tab = new TabItem()
            {
                Header = Call.FileSelected.Name,
                ToolTip = Call.FileSelected.FullName
            };
			tab.MouseDoubleClick += delegate
			{
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
			};
            var instance = new ASM_FINST() {
                OpenFile = Call.FileSelected,
                symbolMap = new Dictionary<ASMChunk, Run>(),
                Tab = tab,
                FileImportData = Call.FileData
            };
            tab.Tag = instance;
            var newEditZone = new ASMCodeEditor(this, instance)
            {
                FontSize = BASE_TEXT_SIZE
            };
            instance.StateObject = newEditZone;
            tab.Content = newEditZone;

            fileInstanceMap.Add(Call.FileSelected.FullName, instance);
            FileBrowserTabView.Items.Add(tab);
            OpenTab(instance);
            await ParseAsync(Call.FileSelected);
        }

		public async Task OpenFileContents(FileInfo FileSelected, ASMFile FileData = default, ASMChunk Symbol = default)
        {
            var call = new FOPENCALL()
            {
                FileSelected = FileSelected,
                FileData = FileData,
                chunk = Symbol,
            };
            if (Paused)
            {
                chewQueue.Enqueue(call);
                return;
            }
            await doFileOpenTaskAsync(call);
        }
        public Task OpenSymbol(ASMChunk chunk) => OpenFileContents(new FileInfo(chunk.OriginalFileName), null, chunk);

        private DispatcherOperation ParseAsync(FileInfo File)
        {
            return Dispatcher.InvokeAsync(async delegate
            {
                await EditorScreen.InvalidateFileContents();
            });
        }

        private void ButtonZoomRestore_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize = BASE_TEXT_SIZE;
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize--;
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize+=1;
        }
    }
}
