using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;

namespace StarFoxMapVisualizer.Misc
{
	public enum ASMLineType
	{
		Unknown,
		Label,
		Define,
		Macro,
		MacroInvoke,
		MacroInvokeParameter
	}

	/// <summary>
	/// Represents a Highlighting tip
	/// </summary>
	internal sealed class HighlightDesc
	{
		private static readonly IReadOnlyDictionary<ASMLineType, Brush> FallbackBrushes = InitFallbackBrushes();

		private static IReadOnlyDictionary<ASMLineType, Brush> InitFallbackBrushes()
		{
			var dicBrushes = new Dictionary<ASMLineType, Brush>(4);
			dicBrushes.Add(ASMLineType.Define, Brushes.Red);
			dicBrushes.Add(ASMLineType.MacroInvoke, Brushes.Orange);
			dicBrushes.Add(ASMLineType.MacroInvokeParameter, Brushes.Pink);
			dicBrushes.Add(ASMLineType.Macro, Brushes.Red);
			return new ReadOnlyDictionary<ASMLineType, Brush>(dicBrushes);
		}

		public readonly string Word;

		public readonly Brush HighlightKey;

		/// <summary>
		/// If this highlight is due to ASMCodeDom information, provide the source of the information
		/// </summary>
		public readonly ASMChunk ChunkHint;

		/// <summary>
		/// INDEX MODE is when this is not -1. It will highlight everything after the index of this character with the value of Word.
		/// </summary>
		public char? Index = null;

		/// <summary>
		/// Sets a generic tooltip message on this highlighted symbol. ChunkHint tooltip will not be applied if this isn't null.
		/// </summary>
		public string TooltipText = null;

		public HighlightDesc(string word, FrameworkElement finder, ASMLineType highlightKey, ASMChunk chunkHint)
		{
			if (!FallbackBrushes.ContainsKey(highlightKey)) {
				throw new InvalidEnumArgumentException(nameof(highlightKey), (int)highlightKey, typeof(ASMLineType));
			}
			this.Word = word;
			this.HighlightKey = finder.FindResource(highlightKey + "Color") as Brush ?? FallbackBrushes[highlightKey];
			this.ChunkHint = chunkHint;
		}

		public override string ToString()
		{
			if (this.TooltipText != null) {
				return this.TooltipText;
			} else if (this.ChunkHint != null) {
				return this.ChunkHint.ToString("G", null);
			} else {
				return "";
			}
		}

		public SolidColorBrush TooltipForeground => (this.TooltipText == null) ? Brushes.White : Brushes.Black;

		public SolidColorBrush TooltipBackground
		{
			get {
				if (this.TooltipText != null) {
					return Brushes.White;
				} else if (this.ChunkHint is ASMMacro) {
					return new SolidColorBrush(Color.FromRgb(0x00, 0x4F, 0x69));
				} else if (this.ChunkHint is ASMConstant) {
					return new SolidColorBrush(Color.FromRgb(0x33, 0x00, 0x7F));
				} else {
					return null;
				}
			}
		}

		public TextBlock ToTextBlock()
		{
			var block = new TextBlock();
			if (this.TooltipText != null) {
				block.Inlines.Add(new Run(this.TooltipText));
			} else {
				var namedSymbol = this.ChunkHint as IASMNamedSymbol;
				if (namedSymbol != null) {
					AsmChunkTextBlock.FormatNamedChunk(block, namedSymbol, this.ChunkHint);
				}
			}

			return block;
		}
	}

	internal static class AsmChunkTextBlock
	{
		internal static TextBlock ToTextBlock<T>(this T namedAsmChunk) where T : ASMChunk, IASMNamedSymbol
		{
			var block = new TextBlock();
			FormatNamedChunk(block, namedAsmChunk, namedAsmChunk);
			return block;
		}

		internal static void FormatNamedChunk(TextBlock block, IASMNamedSymbol namedSymbol, ASMChunk chunkHint)
		{
			block.Inlines.Add(MakeRun(namedSymbol.Name, "#Star Fox/Starwing", 12));
			block.Inlines.Add(new LineBreak());
			block.Inlines.Add(MakeRun(chunkHint.ToString("F0", null), "#Atlantis International", 22));
		}

		private static Run MakeRun(string text, string fontFace, int fontSize)
		{
			var run = new Run(text);
			run.FontFamily = fontFace[0] == '#' ?
			 new FontFamily(new Uri("pack://application:,,,/Resources/ControlStyle.xaml"), "/Resources/" + fontFace) :
			 new FontFamily(fontFace);
			run.FontSize = fontSize;
			return run;
		}
	}
}
