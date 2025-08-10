using System.Collections.Generic;
using System.Xml;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace TextEditLib.Foldings
{
	public class DecoratedXmlFoldingStrategy : IFoldingStrategy
	{
		private XmlFoldingStrategy m_rawStrategy = new XmlFoldingStrategy();

		public bool ShowAttributesWhenFolded
		{
			get { return m_rawStrategy.ShowAttributesWhenFolded; }
			set { m_rawStrategy.ShowAttributesWhenFolded = value; }
		}

		public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			return m_rawStrategy.CreateNewFoldings(document, out firstErrorOffset);
		}

		public void UpdateFoldings(FoldingManager manager, TextDocument document)
		{
			m_rawStrategy.UpdateFoldings(manager, document);
		}

		public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, XmlReader reader, out int firstErrorOffset)
		{
			return m_rawStrategy.CreateNewFoldings(document, reader, out firstErrorOffset);
		}
	}
}
