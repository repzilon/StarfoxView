using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using ICSharpCode.AvalonEdit.Rendering;
using TextEditLib.Extensions;
using TextEditLib.Foldings;

namespace TextEditLib
{
	/// <summary>
	/// Implements an AvalonEdit control textedit control with extensions.
	/// </summary>
	public class TextEdit : TextEditor
	{
		#region fields
		#region EditorCurrentLine Highlighting Colors
		private static readonly DependencyProperty EditorCurrentLineBackgroundProperty =
			DependencyProperty.Register("EditorCurrentLineBackground",
										 typeof(Brush),
										 typeof(TextEdit),
										 new UIPropertyMetadata(new SolidColorBrush(Colors.Transparent)));

		public static readonly DependencyProperty EditorCurrentLineBorderProperty =
			DependencyProperty.Register("EditorCurrentLineBorder", typeof(Brush),
				typeof(TextEdit), new PropertyMetadata(new SolidColorBrush(
					Color.FromArgb(0x60, SystemColors.HighlightBrush.Color.R,
										 SystemColors.HighlightBrush.Color.G,
										 SystemColors.HighlightBrush.Color.B))));

		public static readonly DependencyProperty EditorCurrentLineBorderThicknessProperty =
			DependencyProperty.Register("EditorCurrentLineBorderThickness", typeof(double),
				typeof(TextEdit), new PropertyMetadata(2.0d));
		#endregion EditorCurrentLine Highlighting Colors

		public new static readonly DependencyProperty SyntaxHighlightingProperty =
			TextEditor.SyntaxHighlightingProperty.AddOwner(typeof(TextEdit),
				new FrameworkPropertyMetadata(OnSyntaxHighlightingChanged));

		/// <summary>
		/// Document property.
		/// </summary>
		public new static readonly DependencyProperty DocumentProperty
			= TextView.DocumentProperty.AddOwner(
				typeof(TextEdit), new FrameworkPropertyMetadata(OnDocumentChanged));

		FoldingManager mFoldingManager = null;
		IFoldingStrategy mFoldingStrategy = null;
		#endregion fields

		#region ctors
		/// <summary>
		/// Static class constructor
		/// </summary>
		static TextEdit()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEdit),
				new FrameworkPropertyMetadata(typeof(TextEdit)));
		}

		/// <summary>
		/// Class constructor
		/// </summary>
		public TextEdit()
		{
			this.Loaded += TextEdit_Loaded;
			CommandBindings.Add(new CommandBinding(TextEditCommands.FoldsCollapseAll, TextEdit.FoldsCollapseAll, TextEdit.FoldsCollapseExpandCanExecute));
			CommandBindings.Add(new CommandBinding(TextEditCommands.FoldsExpandAll, TextEdit.FoldsExpandAll, TextEdit.FoldsCollapseExpandCanExecute));
		}
		#endregion ctors

		#region properties
		#region EditorCurrentLine Highlighting Colors
		/// <summary>
		/// Style the background color of the current editor line
		/// </summary>
		public Brush EditorCurrentLineBackground
		{
			get { return (Brush)GetValue(EditorCurrentLineBackgroundProperty); }
			set { SetValue(EditorCurrentLineBackgroundProperty, value); }
		}

		public Brush EditorCurrentLineBorder
		{
			get { return (Brush)GetValue(EditorCurrentLineBorderProperty); }
			set { SetValue(EditorCurrentLineBorderProperty, value); }
		}

		public double EditorCurrentLineBorderThickness
		{
			get { return (double)GetValue(EditorCurrentLineBorderThicknessProperty); }
			set { SetValue(EditorCurrentLineBorderThicknessProperty, value); }
		}
		#endregion EditorCurrentLine Highlighting Colors
		#endregion properties

		#region methods
		/// <summary>
		/// Method is invoked when the control is loaded for the first time.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextEdit_Loaded(object sender, RoutedEventArgs e)
		{
			AdjustCurrentLineBackground();
		}

		/// <summary>
		/// Reset the <seealso cref="SolidColorBrush"/> to be used for highlighting the current editor line.
		/// </summary>
		private void AdjustCurrentLineBackground()
		{
			HighlightCurrentLineBackgroundRenderer oldRenderer = null;

			// Make sure there is only one of this type of background renderer
			// Otherwise, we might keep adding and WPF keeps drawing them on top of each other
			foreach (var item in this.TextArea.TextView.BackgroundRenderers) {
				if (item is HighlightCurrentLineBackgroundRenderer renderer) {
					oldRenderer = renderer;
				}
			}

			if (oldRenderer != null) {
				this.TextArea.TextView.BackgroundRenderers.Remove(oldRenderer);
			}

			this.TextArea.TextView.BackgroundRenderers.Add(new HighlightCurrentLineBackgroundRenderer(this));
		}

		private static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEdit = d as TextEdit;
			textEdit?.OnChangedFoldingInstance(e);
		}

		private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEdit = d as TextEdit;
			textEdit?.OnDocumentChanged(e);
		}

		private void OnDocumentChanged(DependencyPropertyChangedEventArgs e)
		{
			// Clean up and re-install foldings to avoid exception 'Invalid Document' being thrown by StartGeneration
			OnChangedFoldingInstance(e);
		}

		/// <summary>
		/// Method is invoked when the Document or SyntaxHighlightingDefinition dependency property is changed.
		/// This change should always lead to removing and re-installing the correct folding manager and strategy.
		/// </summary>
		/// <param name="e"></param>
		private void OnChangedFoldingInstance(DependencyPropertyChangedEventArgs e)
		{
			try {
				// Clean up last installation of folding manager and strategy
				if (mFoldingManager != null) {
					FoldingManager.Uninstall(mFoldingManager);
					mFoldingManager = null;
				}

				this.mFoldingStrategy = null;
			} catch (Exception) {
				// ignore
			}

			var syntaxHighlighting = e.NewValue as IHighlightingDefinition;
			if (syntaxHighlighting == null) {
				return;
			}

			var strSyntax = syntaxHighlighting.Name;
			IIndentationStrategy indenter = new DefaultIndentationStrategy();

			if (strSyntax == "SuperFX and 65c816 Assembly (Argonaut syntax)") { 
				mFoldingStrategy = new ArgonautFoldingStrategy();
			} else if (strSyntax == "XML") {
				mFoldingStrategy = new DecoratedXmlFoldingStrategy();
			} else if (strSyntax == "C#" || strSyntax == "C/C++" || strSyntax == "PHP" || strSyntax == "Java") {
				indenter = new CSharpIndentationStrategy(this.Options);
				mFoldingStrategy = new BraceFoldingStrategy();
			} else {
				mFoldingStrategy = null;
			}

			this.TextArea.IndentationStrategy = indenter;
			if (mFoldingStrategy != null) {
				if (mFoldingManager == null) {
					mFoldingManager = FoldingManager.Install(this.TextArea);
				}

				UpdateFoldings();
			} else if (mFoldingManager != null) {
				FoldingManager.Uninstall(mFoldingManager);
				mFoldingManager = null;
			}
		}

		private void UpdateFoldings()
		{
			if (mFoldingStrategy != null) {
				mFoldingStrategy.UpdateFoldings(mFoldingManager, this.Document);
			}
		}

		#region Fold Unfold Command
		/// <summary>
		/// Determines whether a folding command can be executed or not and sets corresponding
		/// <paramref name="e"/>.CanExecute property value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void FoldsCollapseExpandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
			e.Handled = true;

			var editoredi = sender as TextEdit;

			if (editoredi?.mFoldingManager?.AllFoldings == null) {
				return;
			}

			e.CanExecute = true;
		}

		/// <summary>
		/// Executes the collapse all folds command (which folds all text foldings but the first).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void FoldsCollapseAll(object sender, ExecutedRoutedEventArgs e)
		{
			var editor = sender as TextEdit;
			editor?.CollapseAllTextfoldings();
		}

		/// <summary>
		/// Executes the collapse all folds command (which folds all text foldings but the first).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void FoldsExpandAll(object sender, ExecutedRoutedEventArgs e)
		{
			var editor = sender as TextEdit;
			editor?.ExpandAllTextFoldings();
		}

		/// <summary>
		/// Goes through all foldings in the displayed text and folds them
		/// so that users can explore the text in a top-down manner.
		/// </summary>
		private void CollapseAllTextfoldings()
		{
			if (this.mFoldingManager?.AllFoldings == null) {
				return;
			}

			foreach (var loFolding in this.mFoldingManager.AllFoldings) {
				loFolding.IsFolded = true;
			}

			// Unfold the first fold (if any) to give a useful overview on content
			var foldSection = this.mFoldingManager.GetNextFolding(0);

			if (foldSection != null) {
				foldSection.IsFolded = false;
			}
		}

		/// <summary>
		/// Goes through all foldings in the displayed text and unfolds them
		/// so that users can see all text items (without having to play with folding).
		/// </summary>
		private void ExpandAllTextFoldings()
		{
			if (this.mFoldingManager?.AllFoldings == null) {
				return;
			}

			foreach (var loFolding in this.mFoldingManager.AllFoldings) {
				loFolding.IsFolded = false;
			}
		}
		#endregion Fold Unfold Command
		#endregion methods
	}
}
