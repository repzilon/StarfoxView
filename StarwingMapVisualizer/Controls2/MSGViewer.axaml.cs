using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using StarFox.Interop.MSG;
using static StarwingMapVisualizer.Controls2.CommunicationMessageControl;

namespace StarwingMapVisualizer.Controls2
{
	public partial class MSGViewer : UserControl
	{
		private Timer animationTimer;
		private Dictionary<string, IEnumerable<MSGEntry>> fileMap = new Dictionary<string, IEnumerable<MSGEntry>>();
		public string SelectedFileName { get; private set; }
		private Characters CurrentSpeaker = Characters.FOX;
		MSGEntry currentMessage;

		public MSGViewer()
		{
			InitializeComponent();
			Loaded           += MSGViewer_Loaded;
			this.SizeChanged += MSGViewer_SizeChanged;
		}

		private void MSGViewer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ResizeActorColumns();
		}

		private void ResizeActorColumns()
		{
			var parentScroller = (ScrollViewer)MessagesItemsHost.Parent;
			var height         = parentScroller.Height;
			if (!Double.IsNaN(height)) {
				height = Math.Max(89, height - 25); // -25 for the horizontal scroll bar below
				foreach (var control in MessagesItemsHost.Children) {
					((HeaderedContentControl)control).MaxHeight = height;
				}
			}
		}

		/// <summary>
		/// Fox prompts the user with instructions
		/// </summary>
		/// <param name="Prompt"></param>
		/// <returns></returns>
		private DispatcherOperation ClearUIMessages(string Prompt = "select a file!!")
		{
			return Dispatcher.UIThread.InvokeAsync(delegate { MugshotControl.Content = Prompt; });
		}

		private void MSGViewer_Loaded(object sender, RoutedEventArgs e)
		{
			InvokeAnimation();
			ResizeActorColumns();
		}

		/// <summary>
		/// Refreshes the files included in this view
		/// </summary>
		public async Task RefreshFiles()
		{
			MessagesItemsHost.Children.Clear();
			await ClearUIMessages(); // fox prompts to select a file!!
			fileMap.Clear();
			foreach (MSGFile messages in AppResources.OpenFiles.Values.OfType<MSGFile>()) {
				fileMap.Add(Path.GetFileNameWithoutExtension(messages.OriginalFilePath), messages.Entries.Values);
			}

			RefreshUI();
		}

		private void RefreshUI()
		{
			FilesCombo.SelectionChanged -= SourceFileChanged;
			FilesCombo.Items.Clear();
			foreach (var file in fileMap) {
				FilesCombo.Items.Add(file.Key);
			}

			FilesCombo.SelectionChanged += SourceFileChanged;
			// Invokes SourceFileChanged here to be the first the file
			FilesCombo.SelectedIndex = FilesCombo.Items.Count > 0 ? 0 : -1;
		}

		private async void SourceFileChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedFileName = (string)FilesCombo.SelectedItem;
			if (!fileMap.ContainsKey(SelectedFileName)) {
				return; // YIKES -- the file isn't in our collection because RefreshFiles hasn't been called
			}

			//**REFRESH UI MESSAGES
			await ClearUIMessages("pick a message!!"); // have fox prompt the user to pick a message
			MessagesItemsHost.Children.Clear();
			var personToListBoxMap = new Dictionary<string, ListBox>();

			var i = 1;
			foreach (var message in fileMap[SelectedFileName]) {
				if (message != null) {
					AddMessage(personToListBoxMap, message, i);
					i++;
				}
			}

			ResizeActorColumns();
		}

		private void AddMessage(Dictionary<string, ListBox> personToListBoxMap, MSGEntry Entry, int messageNumber)
		{
			const int kCroppedLength = 35;

			if (!personToListBoxMap.TryGetValue(Entry.Speaker, out ListBox listBox)) {
				// we haven't created UI containers for this person yet
				listBox                  =  new ListBox(); // make a message list
				listBox.SelectionChanged += MessageChanged;
				var itemHost = new HeaderedContentControl() { // make a container for the message list
					Header  = Entry.Speaker,
					Content = listBox
				};
				MessagesItemsHost.Children.Add(itemHost); // add the host
				personToListBoxMap.Add(Entry.Speaker, listBox);
			}

			string cStr = $"{messageNumber}: {Entry.English}";
			listBox.Items.Add(new ListBoxItem() {
				Content = cStr.Substring(0, Math.Min(cStr.Length, kCroppedLength)),
				Tag     = Entry
			});
		}

		private void MessageChanged(object sender, SelectionChangedEventArgs e)
		{
			var messageEntry = (((ListBox)sender).SelectedItem as ListBoxItem).Tag as MSGEntry;
			MessageChanged(messageEntry);
		}

		private async void MessageChanged(MSGEntry Entry)
		{
			if (Entry == null) {
				//**REFRESH UI MESSAGES
				await ClearUIMessages("pick a message!!"); // have fox prompt the user to pick a message
				return;
			}

			var messageEntry = currentMessage = Entry;
			var mugshotText  = EnglishButton.IsChecked ?? false ? messageEntry.English : messageEntry.SecondaryLanguage;
			if (!messageEntry.Speaker.EndsWith("3")) {
				mugshotText = Environment.NewLine + mugshotText;
			}

			MugshotControl.Content = mugshotText;
			MugshotControl.SetCompatibleFonts(!EnglishButton.IsChecked ?? true);
			CurrentSpeaker  = MapSpeakerToCharacter(messageEntry.Speaker);
			SoundLabel.Text = messageEntry.Sound;
			InvokeAnimation();
		}

		private DispatcherOperation RedrawMugshot(int frame)
		{
			return Dispatcher.UIThread.InvokeAsync(delegate { MugshotControl.DrawMugshot(CurrentSpeaker, frame); });
		}

		private void InvokeAnimation()
		{
			int dueTime  = 50;
			int loops    = 0;
			int maxLoops = 31;

			async void Callback(object state)
			{
				if (animationTimer == null) {
					return;
				}

				loops++;
				if (loops >= maxLoops) {                 // we've met the max animations, lets close up
#if NETFRAMEWORK
					animationTimer.Dispose();
#else
					await animationTimer.DisposeAsync(); // get rid of it
#endif
					animationTimer = null;
					return;
				}

				int frame = loops % 2; // pick frame of animation based on if animation timer is even
				await RedrawMugshot(frame);
			}

			if (animationTimer == null) {
				animationTimer = new Timer(Callback, null, dueTime, dueTime); // create timer since none exists rn
			} else {
				loops = 0; // reset animation again
			}
		}

		private void EnglishButton_Checked(object sender, RoutedEventArgs e)
		{
			MessageChanged(currentMessage);
		}
	}
}
