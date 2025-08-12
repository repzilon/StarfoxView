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
			// line.LineNumber is 1-based, but line numbering in _highlights starts at 0
			if (_highlights.TryGetValue(line.LineNumber - 1, out var lstHighlights)) {
				var c = lstHighlights.Count;
				for (var i = 0; i < c; i++) {
					var keyword = lstHighlights[i].Word;
					if (lstHighlights[i].ChunkHint is ASMMacro) {
						ColorizeKeyword(lineStartOffset, text, keyword, BoldItalic);
					} else if (lstHighlights[i].ChunkHint is ASMConstant) {
						ColorizeKeyword(lineStartOffset, text, keyword, Italic);
					}
				}
			}
		}

		private void ColorizeKeyword(int lineStartOffset, string lineText, string keyword,
		Action<VisualLineElement> fontChanger)
		{
			var start = 0;
			int index;
			while ((index = lineText.IndexOf(keyword, start, StringComparison.InvariantCultureIgnoreCase)) >= 0) {
				var startOffset = lineStartOffset + index;
				base.ChangeLinePart(startOffset, startOffset + keyword.Length, fontChanger);
				start = index + 1; // search for next occurrence
			}
		}

		private static void BoldItalic(VisualLineElement element)
		{
			// Replace the typeface with a modified version of the same typeface
			element.TextRunProperties.SetTypeface(Italic(element.TextRunProperties.Typeface, FontWeights.Bold));
		}

		private static void Italic(VisualLineElement element)
		{
			var tf = element.TextRunProperties.Typeface;
			// Replace the typeface with a modified version of the same typeface
			element.TextRunProperties.SetTypeface(Italic(tf, tf.Weight));
		}

		private static Typeface Italic(Typeface tf, FontWeight weight)
		{
			return new Typeface(tf.FontFamily, FontStyles.Italic, weight, tf.Stretch);
		}
	}
}
