using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using HL.Manager;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFoxMapVisualizer.Misc;
using TextEditLib.Extensions;
using TextEditLib.Themes;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
	/// <summary>
	/// Interaction logic for ASMAvalonEditor.xaml
	/// </summary>
	public partial class AsmAvalonEditor : TextEditLib.TextEdit
	{
		private readonly ASMControl _parent; // attached parent control
		private ASM_FINST<AsmAvalonEditor> FileInstance { get; } // the current context for this control
		private Dictionary<ASMChunk, Run> SymbolMap => FileInstance?.SymbolMap; // where all the symbols in the document are located
		private IEnumerable<ASMMacro> _macros; // performance cache
		private IEnumerable<string> _macroNames; // performance cache

		private readonly ToolTip _wpfToolTip = new ToolTip() { HasDropShadow = true };
		private IDictionary<int, IList<HighlightDesc>> Highlights;

		#region Constructors
		static AsmAvalonEditor()
		{
			// Do not use the "Dark" theme yet because it lacks the control color style.
			// Right now, you would get dark syntax highlighting over a white background; not legible.
			DefaultHighlightingManager.Instance.SetCurrentTheme(
			 DefaultHighlightingManager.BuiltinThemes.VisualStudio2019Dark);
		}

		public AsmAvalonEditor()
		{
			InitializeComponent();
			this.MouseHover += AsmAvalonEditor_MouseHover;
			this.MouseHoverStopped += AsmAvalonEditor_MouseHoverStopped;
			this.TextArea.SelectionChanged += TextArea_SelectionChanged;
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
		#endregion

		#region Toolip display
		private void AsmAvalonEditor_MouseHoverStopped(object sender, MouseEventArgs e)
		{
			_wpfToolTip.IsOpen = false;
		}

		private void AsmAvalonEditor_MouseHover(object sender, MouseEventArgs e)
		{
			var pos = GetPositionFromPoint(e.GetPosition(this));
			if (pos != null) {
				var lineNumber = pos.Value.Line;
				IList<HighlightDesc> lstHilites;
				if (Highlights.TryGetValue(lineNumber, out lstHilites)) {
					var hoveredWord = GetWordAtMousePosition(e);
					var desc = lstHilites.FirstOrDefault(x => x.Word == hoveredWord);
					if (desc != null) {
						_wpfToolTip.Background = desc.TooltipBackground;
						_wpfToolTip.Foreground = desc.TooltipForeground;
						_wpfToolTip.Content = desc.ToTextBlock();

						if (desc.ChunkHint != null) {
							var highlight = new Run(desc.Word)
							{
								Foreground = desc.HighlightKey
							};

							if (!SymbolMap.TryGetValue(desc.ChunkHint, out var symbolLocation)) {
								SymbolMap.Add(desc.ChunkHint, highlight);
							} else if (symbolLocation == null) {    // register symbol into the map
								SymbolMap[desc.ChunkHint] = highlight;
							}
						}

						_wpfToolTip.PlacementTarget = this; // required for property inheritance
						_wpfToolTip.IsOpen = desc.TooltipBackground != null;
					}
				}

				e.Handled = true;
			}
		}

		// https://stackoverflow.com/questions/27003784/select-word-like-double-click-in-avalonedit
		private string GetWordAtMousePosition(MouseEventArgs e)
		{
			var mousePosition = this.GetPositionFromPoint(e.GetPosition(this));

			if (mousePosition == null) {
				return "";
			}

			var mpv = mousePosition.Value;
			var offset = Document.GetOffset(mpv.Line, mpv.Column);

			if (offset >= Document.TextLength) {
				offset--;
			}

			var offsetStart = TextUtilities.GetNextCaretPosition(Document, offset, LogicalDirection.Backward, CaretPositioningMode.WordBorder);
			var offsetEnd = TextUtilities.GetNextCaretPosition(Document, offset, LogicalDirection.Forward, CaretPositioningMode.WordBorder);

			if (offsetEnd == -1 || offsetStart == -1) {
				return "";
			}

			var currentChar = Document.GetText(offset, 1);

			return String.IsNullOrWhiteSpace(currentChar) ? "" : Document.GetText(offsetStart, offsetEnd - offsetStart);
		}
		#endregion

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
			if (FileInstance == null) {
				return;
			}

			InvalidateMacros();

			var lineNumber = -1;
			var dicHighlights = new Dictionary<int, IList<HighlightDesc>>();
			using (var fs = FileInstance.OpenFile.OpenText()) {
				// open file for reading
				while (!fs.EndOfStream) {
					var line = fs.ReadLine();
					lineNumber++;   // line numbering similar to AvalonEdit
					ProcessLineForTooltips(dicHighlights, line, lineNumber);
				}
			}
			this.Highlights = dicHighlights;

			using (var stream = FileInstance.OpenFile.OpenRead()) {
				base.Load(stream);
			}
			base.SyntaxHighlighting = DefaultHighlightingManager.Instance.GetDefinitionByExtension(
			 Path.GetExtension(FileInstance.OpenFile.Name));
			ApplyHighlightManagerThemeToControl();
			ReinstallLineTransformer(new ContextualKeywordStyler(this.Highlights), true);
		}

		/// <summary>
		/// Shows and highlights string content
		/// </summary>
		/// <param name="line"></param>
		public void ShowStringContent(string line)
		{
			if (!String.IsNullOrWhiteSpace(line)) {
				using (var ms = new MemoryStream()) {
					using (var stw = new StreamWriter(ms)) {
						stw.WriteLine(line);
					}
					ms.Flush();
					base.Load(ms);
				}
				base.SyntaxHighlighting = DefaultHighlightingManager.Instance.GetDefinitionByExtension(".asm");

				var dicHighlights = new Dictionary<int, IList<HighlightDesc>>();
				ProcessLineForTooltips(dicHighlights, line, 1);
				this.Highlights = dicHighlights;
			}
		}

		/// <summary>
		/// Jumps to the given symbol, if it is present in this document
		/// </summary>
		/// <param name="chunk"></param>
		public bool ScrollToSymbol(ASMChunk chunk)
		{
			if ((chunk == null) || (chunk.OriginalFileName != FileInstance.OpenFile.FullName)) {
				return false;
			}

			if (SymbolMap?.TryGetValue(chunk, out var run) ?? false) {
				ScrollToInline(run);
				return true;
			} else {
				ScrollToLine(chunk.Line);
				return true;
			}
		}

		private void ScrollToInline(Inline rtfInline)
		{
			if (rtfInline != null) {
				var start = rtfInline.ContentStart;
				var characterRect = start.GetCharacterRect(LogicalDirection.Forward);
				ScrollToHorizontalOffset(HorizontalOffset + characterRect.Left - ActualWidth / 2d);
				ScrollToVerticalOffset(VerticalOffset + characterRect.Top - ActualHeight / 2d);
				try {
					this.CaretOffset = Math.Abs(start.GetOffsetToPosition(start.DocumentStart));
				} catch (ArgumentOutOfRangeException) {
					// ignore
				}
			}
		}

		#region Line processing for tooltips
		private void ProcessLineForTooltips(IDictionary<int, IList<HighlightDesc>> destination,
		string line, int lineNumber)
		{
			if (!String.IsNullOrWhiteSpace(line)) {
				var highlights = FindHighlights(line, (uint)lineNumber).ToList(); // find the big words
				if (highlights.Count > 0) {
					destination.Add(lineNumber, highlights);
				}
			}
		}

		/// <summary>
		/// Searches through a line to find symbols and keywords.
		/// <para>This uses data from sources like <see cref="FINST.FileImportData"/> to link symbols.</para>
		/// </summary>
		/// <param name="input"></param>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		private IEnumerable<HighlightDesc> FindHighlights(string input, uint lineNumber)
		{
			var lineBlocks = input.Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			// DEFINES
			var parsedLine = FileInstance?.FileImportData?.Chunks.OfType<ASMLine>().FirstOrDefault(x => x.Line == lineNumber);
			if (parsedLine != null && parsedLine.HasStructureApplied) { // line found and it has recognizable structure
				var structure = parsedLine.StructureAsDefineStructure;  // is this structure a define structured line?
				if (structure != null) {
					if (structure.Constant != null && !SymbolMap.ContainsKey(structure.Constant)) {
						SymbolMap.Add(structure.Constant, null);
					}

					yield return NewHighlightDesc(structure.Symbol, ASMLineType.Define, structure.Constant);
					yield return NewHighlightDesc(structure.Value, ASMLineType.MacroInvokeParameter, null,
						$"Value: {structure.Value}");
					yield break;
				}
			}
			if (lineBlocks.Length > 2 && lineBlocks[1].ToLower().Contains("equ")) { // define found
				yield return NewHighlightDesc(lineBlocks[0], ASMLineType.Define, null);
				yield break;
			}
			//MACROS
			if (parsedLine != null && parsedLine.HasStructureApplied) {     // line found and it has recognizable structure
				var structure = parsedLine.StructureAsMacroInvokeStructure; // is this structure a macro invoke structured line?
				if (structure != null) {
					yield return NewHighlightDesc(structure.MacroReference.Name, ASMLineType.MacroInvoke,
						structure.MacroReference);
					var index = 0;
					foreach (var param in structure.Parameters) {
						index++;
						yield return NewHighlightDesc(param.ParameterContent, ASMLineType.MacroInvokeParameter, null,
							$"Parameter {index}: {param.ParameterName}");
					}
					yield break;
				}
			}
			foreach (var block in lineBlocks) { // check each word
				if (_macroNames.Contains(block.ToLower())) { // macro found
					var sourceMacroData = _macros.FirstOrDefault(x => x.Name == block);
					if (sourceMacroData != null && !SymbolMap.ContainsKey(sourceMacroData)) {
						SymbolMap.Add(sourceMacroData, null);
					}
					yield return NewHighlightDesc(block, ASMLineType.Macro, sourceMacroData);
				}
			}
		}

		private HighlightDesc NewHighlightDesc(string word, ASMLineType highlightKey, ASMChunk chunk)
		{
			return new HighlightDesc(word, this, highlightKey, chunk);
		}

		private HighlightDesc NewHighlightDesc(string word, ASMLineType highlightKey, ASMChunk chunk, string tooltip)
		{
			var x = new HighlightDesc(word, this, highlightKey, chunk);
			x.TooltipText = tooltip;
			return x;
		}

		/// <summary>
		/// Set Keyboard Macro Shortcuts on this particular symbol
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="chunk"></param>
		private void SetupKeyboardMacros(Run keyword, ASMChunk chunk)
		{
			void Clicked(object sender, MouseButtonEventArgs e)
			{
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) { // NAVIGATE COMMAND
					_parent.OpenSymbol(chunk);
				}
			}
			keyword.Cursor = Cursors.Hand;
			keyword.MouseLeftButtonDown += Clicked;
		}
		#endregion

		#region Control theming from ThemedDemo
		private void ApplyHighlightManagerThemeToControl()
		{
			var hlTheme = DefaultHighlightingManager.Instance.CurrentTheme.HlTheme;
			// Does this highlighting definition have an associated highlighting theme?
			if (hlTheme != null) {
				// A highlighting theme with GlobalStyles? Apply these styles to the resource keys of the editor
				foreach (var item in hlTheme.GlobalStyles) {
					if (item.TypeName == "DefaultStyle") {
						ApplyToDynamicResource(ResourceKeys.EditorBackground, item.backgroundcolor);
						ApplyToDynamicResource(ResourceKeys.EditorForeground, item.foregroundcolor);
					} else if (item.TypeName == "CurrentLineBackground") {
						ApplyToDynamicResource(ResourceKeys.EditorCurrentLineBackgroundBrushKey, item.backgroundcolor);
						ApplyToDynamicResource(ResourceKeys.EditorCurrentLineBorderBrushKey, item.bordercolor);
					} else if (item.TypeName == "LineNumbersForeground") {
						ApplyToDynamicResource(ResourceKeys.EditorLineNumbersForeground, item.foregroundcolor);
					} else if (item.TypeName == "Selection") {
						ApplyToDynamicResource(ResourceKeys.EditorSelectionBrush, item.backgroundcolor);
						ApplyToDynamicResource(ResourceKeys.EditorSelectionBorder, item.bordercolor);
					} else if (item.TypeName == "Hyperlink") {
						ApplyToDynamicResource(ResourceKeys.EditorLinkTextBackgroundBrush, item.backgroundcolor);
						ApplyToDynamicResource(ResourceKeys.EditorLinkTextForegroundBrush, item.foregroundcolor);
					} else if (item.TypeName == "NonPrintableCharacter") {
						ApplyToDynamicResource(ResourceKeys.EditorNonPrintableCharacterBrush, item.foregroundcolor);
					} else {
						throw new NotSupportedException("GlobalStyle named '" + item.TypeName + "' is not supported.");
					}
				}
			}
		}

		/// <summary>
		/// Re-define an existing <seealso cref="SolidColorBrush"/>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newColor"></param>
		private static void ApplyToDynamicResource(ComponentResourceKey key, Color? newColor)
		{
			if (newColor != null) {
				if ((Application.Current.Resources[key] == null) ||
				(Application.Current.Resources[key] is SolidColorBrush)) {
					var newColorBrush = new SolidColorBrush((Color)newColor);
					newColorBrush.Freeze();
					Application.Current.Resources[key] = newColorBrush;
				}
			}
		}
		#endregion

		#region Dynamic highlighting
		// https://stackoverflow.com/questions/9223674/highlight-all-occurrences-of-selected-word-in-avalonedit
		private void TextArea_SelectionChanged(object sender, EventArgs e)
		{
			this.ReinstallLineTransformer(new MarkSameWord(SelectedText), !String.IsNullOrWhiteSpace(SelectedText));
		}

		private void ReinstallLineTransformer<T>(T newInstance, bool condition) where T : DocumentColorizingTransformer
		{
			var transformers = TextArea.TextView.LineTransformers;
			foreach (var oldTransformer in transformers.OfType<T>().ToList()) {
				transformers.Remove(oldTransformer);
			}
			if (condition) {
				transformers.Add(newInstance);
			}
		}
		#endregion
	}
}
