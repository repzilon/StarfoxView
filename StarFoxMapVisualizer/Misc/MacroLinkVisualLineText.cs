using System.Windows;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls;

namespace StarFoxMapVisualizer.Misc
{
	/// <summary>
	/// VisualLineElement that represents a piece of text and is a clickable link.
	/// </summary>
	public sealed class MacroLinkVisualLineText : VisualLineText
	{
		public ASMControl AssemblyCodeFrame { get; set; }

		public ASMChunk NavigateSymbol { get; set; }

		public bool RequireControlModifierForClick { get; set; }

		public MacroLinkVisualLineText(VisualLine parentVisualLine, int length) :
		base(parentVisualLine, length)
		{
			RequireControlModifierForClick = true;
		}

		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			//TextRunProperties.SetForegroundBrush(Brushes.GreenYellow);
			TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
			TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
			if (context.TextView.LinkTextUnderline) {
				TextRunProperties.SetTextDecorations(TextDecorations.Underline);
			}
			return base.CreateTextRun(startVisualColumn, context);
		}

		private bool LinkIsClickable()
		{
			if ((NavigateSymbol == null) || (this.AssemblyCodeFrame == null)) {
				return false;
			}
			return !RequireControlModifierForClick || (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
		}

		protected override void OnQueryCursor(QueryCursorEventArgs e)
		{
			if (LinkIsClickable()) {
				e.Handled = true;
				e.Cursor = Cursors.Hand;
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left || e.Handled || !LinkIsClickable()) {
				return;
			}
			this.AssemblyCodeFrame.OpenSymbol(NavigateSymbol);
			e.Handled = true;
		}

		protected override VisualLineText CreateInstance(int length)
		{
			return new MacroLinkVisualLineText(ParentVisualLine, length)
			{
				AssemblyCodeFrame = this.AssemblyCodeFrame,
				NavigateSymbol = this.NavigateSymbol,
				RequireControlModifierForClick = RequireControlModifierForClick
			};
		}
	}
}
