using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using StarFox.Interop.ASM.TYP;

namespace StarFoxMapVisualizer.Misc
{
	internal sealed class ContextualKeywordStyler : DocumentColorizingTransformer
	{
		private readonly IReadOnlyDictionary<int, IList<HighlightDesc>> _highlights;

		public ContextualKeywordStyler(IReadOnlyDictionary<int, IList<HighlightDesc>> highlights)
		{
			_highlights = highlights ?? new ReadOnlyDictionary<int, IList<HighlightDesc>>(new Dictionary<int, IList<HighlightDesc>>());
		}

		public ContextualKeywordStyler(IDictionary<int, IList<HighlightDesc>> highlights)
		{
			_highlights = new ReadOnlyDictionary<int, IList<HighlightDesc>>(highlights ?? new Dictionary<int, IList<HighlightDesc>>());
		}

		protected override void ColorizeLine(DocumentLine line)
		{
			var lineStartOffset = line.Offset;
			var text = CurrentContext.Document.GetText(line);
			var start = 0;
			if (_highlights.TryGetValue(line.LineNumber, out var lstHighlights)) {
				var c = lstHighlights.Count;
				for (var i = 0; i < c; i++) {
					if (lstHighlights[i].ChunkHint is ASMMacro) {
						var keyword = lstHighlights[i].Word;
						int index;
						while ((index = text.IndexOf(keyword, start, StringComparison.InvariantCultureIgnoreCase)) >= 0) {
							var startOffset = lineStartOffset + index;
							base.ChangeLinePart(startOffset, startOffset + keyword.Length, BoldItalic);
							start = index + 1; // search for next occurrence
						}
					}
					// TODO : Turn symbolic constants used as macro parameters into normal weight italics
					// Requires improvements to AsmAvalonEditor.FindHighlights method
				}
			}
		}

		private static void BoldItalic(VisualLineElement element)
		{
			var tf = element.TextRunProperties.Typeface;
			// Replace the typeface with a modified version of the same typeface
			element.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, FontStyles.Italic, FontWeights.Bold, tf.Stretch));
		}
	}
}
