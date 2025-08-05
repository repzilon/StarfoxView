using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using HL.Manager;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFoxMapVisualizer.Misc;
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
			base.SyntaxHighlighting = DefaultHighlightingManager.Instance.GetDefinitionByExtension(
			 Path.GetExtension(FileInstance.OpenFile.Name));
			ApplyHighlightManagerThemeToControl();
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
			base.SyntaxHighlighting = DefaultHighlightingManager.Instance.GetDefinitionByExtension(".asm");
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
				ScrollToInline(run);
				return true;
			} else {
				ScrollToLine(chunk.Line);
				return true;
			}
		}

		private void ScrollToInline(Inline rtfInline)
		{
			var characterRect = rtfInline.ContentStart.GetCharacterRect(LogicalDirection.Forward);
			ScrollToHorizontalOffset(HorizontalOffset + characterRect.Left - ActualWidth / 2d);
			ScrollToVerticalOffset(VerticalOffset + characterRect.Top - ActualHeight / 2d);
			// TODO : Change caret position
			//this.CaretOffset = Inline.ContentStart;
		}

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
	}
}
