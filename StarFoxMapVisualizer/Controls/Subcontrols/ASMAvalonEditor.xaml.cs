using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
	/// <summary>
	/// Interaction logic for ASMAvalonEditor.xaml
	/// </summary>
	public partial class AsmAvalonEditor : TextEditor
	{
		private readonly ASMControl _parent; // attached parent control
		private ASM_FINST<AsmAvalonEditor> FileInstance { get; } // the current context for this control
		private Dictionary<ASMChunk, Run> SymbolMap => FileInstance?.SymbolMap; // where all the symbols in the document are located
		private IEnumerable<ASMMacro> _macros; // performance cache
		private IEnumerable<string> _macroNames; // performance cache

		static AsmAvalonEditor()
		{
			IHighlightingDefinition customHighlighting;
			using (Stream s = typeof(AsmAvalonEditor).Assembly.GetManifestResourceStream("StarFoxMapVisualizer.Resources.GSUandSuperNESAssemly.xshd")) {
				if (s == null) {
					throw new InvalidOperationException("Could not find syntax highlighting embedded resource");
				}

				using (XmlReader reader = new XmlTextReader(s)) {
					customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}
			// and register it in the HighlightingManager
			HighlightingManager.Instance.RegisterHighlighting("SuperFX and 65c816 Assembly", 
				new string[] { ".asm", ".inc", ".ext", ".mc" }, customHighlighting);
		}

		public AsmAvalonEditor()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Creates a new ASMCodeEditor attached to the <see cref="ASMControl"/> it's a child of.
		/// <para>This control cannot work without attaching to a <see cref="ASMControl"/></para>
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="fileInstance"></param>
		public AsmAvalonEditor(ASMControl parent, ASM_FINST<AsmAvalonEditor> fileInstance) : this()
		{
			this._parent = parent;
			this.FileInstance = fileInstance;
		}

		public AsmAvalonEditor(string line) : this()
		{
			ShowStringContent(line);
		}

		/// <summary>
		/// Invalidates the macro symbol caches, causing them to be reloaded from <see cref="AppResources.Includes"/>
		/// </summary>
		private void InvalidateMacros()
		{
			_macros = AppResources.Includes.SelectMany(x => x.Chunks.OfType<ASMMacro>()); // get all macros
			_macroNames = _macros.Select(x => x.Name);
		}

		/// <summary>
		/// Invokes the control to reinterpret the file, re-evaluate highlighting tips, and refresh the text in the editor
		/// </summary>
		/// <returns></returns>
		public void InvalidateFileContents()
		{
			if (FileInstance == null) return;

			InvalidateMacros();

			using (var stream = FileInstance.OpenFile.OpenRead()) {
				base.Load(stream);
			}
			base.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(FileInstance.OpenFile.Name));
		}

		/// <summary>
		/// Shows and highlights string content
		/// </summary>
		/// <param name="line"></param>
		public void ShowStringContent(string line)
		{
			using (var ms = new MemoryStream()) {
				using (var stw = new StreamWriter(ms)) {
					stw.WriteLine(line);
				}
				ms.Flush();
				base.Load(ms);
			}
			// TODO : Single line syntax highlighting
			//base.SyntaxHighlighting = 
		}

		/// <summary>
		/// Jumps to the given symbol, if it is present in this document
		/// </summary>
		/// <param name="chunk"></param>
		public bool ScrollToSymbol(ASMChunk chunk)
		{
			if (chunk == null) return false;
			if (chunk.OriginalFileName != FileInstance.OpenFile.FullName) return false;
			if (SymbolMap?.TryGetValue(chunk, out var run) ?? false) {
				//ScrollToInline(run);
				return false;
			} else {
				ScrollToLine(chunk.Line);
				return true;
			}
		}
	}
}
