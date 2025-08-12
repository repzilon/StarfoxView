using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Rendering;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls;
using TextEditLib.Foldings;

namespace StarFoxMapVisualizer.Misc
{
	internal sealed class MacroLinkGenerator : VisualLineElementGenerator
	{
		private readonly ASMControl _codeFrame;

		private readonly IReadOnlyDictionary<int, IList<HighlightDesc>> _highlights;

		public MacroLinkGenerator(ASMControl codeFrame, IReadOnlyDictionary<int, IList<HighlightDesc>> highlights)
		{
			_codeFrame = codeFrame;
			_highlights = highlights ?? new ReadOnlyDictionary<int, IList<HighlightDesc>>(new Dictionary<int, IList<HighlightDesc>>());
		}

		public MacroLinkGenerator(ASMControl codeFrame, IDictionary<int, IList<HighlightDesc>> highlights)
		{
			_codeFrame = codeFrame;
			_highlights = new ReadOnlyDictionary<int, IList<HighlightDesc>>(highlights ?? new Dictionary<int, IList<HighlightDesc>>());
		}

		private KeyValuePair<Match, ASMChunk> FindMatch(int startOffset)
		{
			var lineNumber = CurrentContext.VisualLine.FirstDocumentLine.LineNumber;
			IList<HighlightDesc> lstHighlights;
			if (_highlights.TryGetValue(lineNumber - 1, out lstHighlights)) {
				// fetch the end offset of the VisualLine being generated
				var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
				var relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
				var c = lstHighlights.Count;
				for (var i = 0; i < c; i++) {
					var chunk = lstHighlights[i].ChunkHint;
					if (chunk != null) {
						var match = Regex.Match(relevantText, CaptureWholeWord(lstHighlights[i].Word), RegexOptions.IgnoreCase);
						if (match.Success) {
							return new KeyValuePair<Match, ASMChunk>(match, chunk);
						}
					}
				}
			}

			return new KeyValuePair<Match, ASMChunk>(null, null);
		}

		private static string CaptureWholeWord(string word)
		{
			return @"(?:\s|^)(" + word + @")(?:\s|$)";
		}

		/// <summary>
		/// Gets the first offset >= startOffset where the generator wants to construct an element.
		/// </summary>
		/// <returns>-1 to signal no interest.</returns>
		public override int GetFirstInterestedOffset(int startOffset)
		{
			var m = FindMatch(startOffset);
			return (m.Key != null) && m.Key.Success ? (startOffset + m.Key.Groups[1].Index) : -1;
		}

		/// <summary>Constructs an element at the specified offset.</summary>
		/// <returns>null if no element should be constructed.</returns>
		public override VisualLineElement ConstructElement(int offset)
		{
			var m = FindMatch(offset);
			// check whether there's a match exactly at offset
			if ((m.Key != null) && m.Key.Success && (m.Key.Index == 0)) {
				var link = new MacroLinkVisualLineText(CurrentContext.VisualLine, m.Key.Groups[1].Length);
				link.AssemblyCodeFrame = this._codeFrame;
				link.NavigateSymbol = m.Value;
				return link;
			} else {
				return null;
			}
		}
	}
}
