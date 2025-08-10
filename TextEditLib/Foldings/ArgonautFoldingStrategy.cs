using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace TextEditLib.Foldings
{
	public class ArgonautFoldingStrategy : BaseFoldingStrategy
	{
		private readonly List<KeyValuePair<string, string>> m_lstSpanPatterns;

		public ArgonautFoldingStrategy()
		{
			var lstSpanPatterns = new List<KeyValuePair<string, string>>();
			lstSpanPatterns.Add(new KeyValuePair<string, string>("macro", "endm"));
			lstSpanPatterns.Add(new KeyValuePair<string, string>("if(eq|ne|[gl][te]|[dcvs]|n[dcvs]|fe|fne)", "endc"));
			m_lstSpanPatterns = lstSpanPatterns;
		}

		public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			firstErrorOffset = -1;

			var newFoldings = new List<NewFolding>();
			var n = document.LineCount;
			int l;
			foreach (var kvp in m_lstSpanPatterns) {
				// Navigate the document backwards, looking for the span end first
				var strEndPattern = WholeWord(kvp.Value);
				var strStartPattern = WholeWord(kvp.Key);
				var stkFoldings = new Stack<int>();
				for (l = n - 1; l >= 0; l--) {
					var strLine = document.GetText(document.Lines[l]);
					var intPosOfIgnore = strLine.IndexOfAny("\"<;".ToCharArray());
					var intLineStart = document.Lines[l].Offset;
					var mtcEnd = Regex.Match(strLine, strEndPattern, RegexOptions.IgnoreCase);

					if (mtcEnd.Success) {
						// Check it does not come after a string literal or comment character
						if (NotInsideCommentOrString(intPosOfIgnore, mtcEnd)) {
							stkFoldings.Push(intLineStart + mtcEnd.Index + mtcEnd.Length);
						}
					} else { // end pattern is not found, search for a start pattern
						var mtcStart = Regex.Match(strLine, strStartPattern, RegexOptions.IgnoreCase);
						if (mtcStart.Success && NotInsideCommentOrString(intPosOfIgnore, mtcStart) && (stkFoldings.Count > 0)) {
							newFoldings.Add(new NewFolding(intLineStart + mtcStart.Index, stkFoldings.Pop()));
						}
					}
				}

				if (stkFoldings.Count > 0) { // a non-empty stack means syntax errors
					var stackedError = stkFoldings.Last(); // remember, document is processed backwards
					if ((firstErrorOffset < 0) || (stackedError < firstErrorOffset)) {
						firstErrorOffset = stackedError;
					}
				}
			}

			// Fold comments on consecutive lines, when they start the lines
			var blnInBlockComment = false;
			var intFirstLine = 0;
			for (l = 0; l < n; l++) {	// Navigate forward as we won't have nesting unlike conditionals above
				var strLine = document.GetText(document.Lines[l]);
				if (strLine.StartsWith(";")) {
					if (!blnInBlockComment) {
						intFirstLine = l;
						blnInBlockComment = true;
					}
				} else if (blnInBlockComment) {
					blnInBlockComment = false;
					// No longer in a block comment, previous line was the last of the block
					if (l - 1 > intFirstLine) { // fold comments of >= 2 lines
						newFoldings.Add(new NewFolding(document.Lines[intFirstLine].Offset, document.Lines[l - 1].EndOffset));
					}
				}
			}

			// TODO : label and sublabel folding (I have no idea on how to reliably detect the end of code sequence related to a label)

			newFoldings.Sort(OrderFoldings);
			return newFoldings;
		}

		private static bool NotInsideCommentOrString(int commentOrStringPosition, Match match)
		{
			return (commentOrStringPosition < 0) || (commentOrStringPosition > match.Index + match.Length);
		}

		private static string WholeWord(string word)
		{
			return @"(\s|^)" + word + @"(\s|$)";
		}

		private static int OrderFoldings(NewFolding a, NewFolding b)
		{
			return a.StartOffset.CompareTo(b.StartOffset);
		}
	}
}
