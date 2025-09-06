using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace TextEditLib.Foldings
{
	public interface IFoldingStrategy
	{
		IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset);

		void UpdateFoldings(FoldingManager manager, TextDocument document);
	}

	public abstract class BaseFoldingStrategy : IFoldingStrategy
	{
		public void UpdateFoldings(FoldingManager manager, TextDocument document)
		{
			int firstErrorOffset;
			var newFoldings = CreateNewFoldings(document, out firstErrorOffset);
			manager.UpdateFoldings(newFoldings, firstErrorOffset);
		}

		public abstract IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset);
	}
}
