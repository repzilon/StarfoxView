using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace TextEditLib.Extensions
{
	public class MarkSameWord : DocumentColorizingTransformer
	{
		private readonly string _selectedText;

		public MarkSameWord(string selectedText)
		{
			_selectedText = selectedText;
		}

		protected override void ColorizeLine(DocumentLine line)
		{
			if (String.IsNullOrEmpty(_selectedText)) {
				return;
			}

			var lineStartOffset = line.Offset;
			var text = CurrentContext.Document.GetText(line);
			var start = 0;
			int index;

			while ((index = text.IndexOf(_selectedText, start, StringComparison.Ordinal)) >= 0) {
				var startOffset = lineStartOffset + index;
				ChangeLinePart(startOffset, startOffset + _selectedText.Length, HighlightWord);
				start = index + 1; // search for next occurrence
			}
		}

		private static void HighlightWord(VisualLineElement element)
		{
			var trp = element.TextRunProperties;
			trp.SetBackgroundBrush(Brushes.Yellow);
			trp.SetForegroundBrush(Brushes.Black);
		}
	}
}
