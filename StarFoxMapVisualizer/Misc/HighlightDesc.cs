using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;

namespace StarFoxMapVisualizer.Misc
{
	/// <summary>
	/// Represents a Highlighting tip
	/// </summary>
	internal sealed class HighlightDesc
	{
		private static class ContextualKeyword
		{
			public const string DefineColor = nameof(DefineColor);
			public const string MacroColor = nameof(MacroColor);
			public const string MacroInvokeColor = nameof(MacroInvokeColor);
			public const string MacroInvokeParameterColor = nameof(MacroInvokeParameterColor);
		}

		private static readonly IReadOnlyDictionary<string, Brush> FallbackBrushes = InitFallbackBrushes();

		private static IReadOnlyDictionary<string, Brush> InitFallbackBrushes()
		{
			var dicBrushes = new Dictionary<string, Brush>(4);
			dicBrushes.Add(ContextualKeyword.DefineColor, Brushes.Red);
			dicBrushes.Add(ContextualKeyword.MacroInvokeColor, Brushes.Orange);
			dicBrushes.Add(ContextualKeyword.MacroInvokeParameterColor, Brushes.Pink);
			dicBrushes.Add(ContextualKeyword.MacroColor, Brushes.Red);
			return new ReadOnlyDictionary<string, Brush>(dicBrushes);
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

		public HighlightDesc(string word, FrameworkElement finder, string highlightKey, ASMChunk chunkHint)
		{
			if (!FallbackBrushes.ContainsKey(highlightKey)) {
				throw new ArgumentOutOfRangeException(nameof(highlightKey));
			}
			this.Word = word;
			this.HighlightKey = finder.FindResource(highlightKey) as Brush ?? FallbackBrushes[highlightKey];
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
	}
}
