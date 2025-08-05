using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;

namespace StarFoxMapVisualizer.Controls
{
	/// <summary>
	/// A stylized tooltip that can show information on a Code Object or custom text.
	/// </summary>
	public partial class MacroTooltip : HeaderedContentControl
	{
		/// <summary>
		/// Initializes a basic tooltip with no content
		/// </summary>
		public MacroTooltip()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Generic function -- see documentation on individual members such as:
		/// <see cref="Attach(ASMMacro)"/>, etc.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Chunk"></param>
		public void Attach<T>(T Chunk) where T : ASMChunk
		{
			if (Chunk is ASMMacro macro) Attach(macro);
			else if (Chunk is ASMConstant cons) Attach(cons);
		}

		public void Attach(ASMMacro Macro)
		{
			SetResourceReference(BackgroundProperty, "MacroTooltipColor");
			this.DetailStack.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x4F, 0x69));

			Header = Macro.Name;
			SubtitleText.Text = $"{Macro.ChunkType} defined in {Path.GetFileName(Macro.OriginalFileName)}\n" +
				$"Line: {Macro.Line + 1}, Length: {Macro.Lines.Length} lines";
			ParameterStack.Children.Clear();
			foreach (var param in Macro.Parameters)
			{
				ParameterStack.Children.Add(new Button()
				{
					IsEnabled = false,
					Content = param
				});
			}
		}

		/// <summary>
		/// Attaches to an <see cref="ASMMacro"/> code object.
		/// <para>Shows Macro Name, Parameters, Callsite, etc.</para>
		/// </summary>
		/// <param name="Constant"></param>
		public void Attach(ASMConstant Constant)
		{
			SetResourceReference(BackgroundProperty, "DefineTooltipColor");
			this.DetailStack.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x00, 0x7F));

			Header = Constant.Name;
			SubtitleText.Text = $"{Constant.ChunkType} defined in {Path.GetFileName(Constant.OriginalFileName)}\n" +
				$"Value: {Constant.Value}";
			ParameterStack.Children.Clear();
		}
	}
}
