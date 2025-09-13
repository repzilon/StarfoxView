using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using StarFox.Interop;
using StarFox.Interop.EFFECTS;
using StarFox.Interop.MAP.CONTEXT;

namespace StarwingMapVisualizer.Renderers
{
	/// <summary>
	/// Interaction logic for BackgroundRenderer.xaml
	/// </summary>
	public partial class BackgroundRenderer : SCRRendererControlBase, IDisposable
	{
		public readonly RelativeRect DefaultViewport = new RelativeRect(0, 0, 512, 512, RelativeUnit.Absolute);

		internal ImageBrush BG2Render
		{
			get { return this.Background2.Fill as ImageBrush; }
		}

		private ImageBrush BG3Render
		{
			get { return this.Background3.Fill as ImageBrush; }
		}

		public BackgroundRenderer()
		{
			InitializeComponent();
#if DEBUG
			DebugViewBlockl.IsVisible = true;
#else
            DebugViewBlockl.IsVisible = false;
#endif
		}

		public override void BG2Invalidate(Bitmap newImage) => BG2Render.Source = newImage;
		public override void BG3Invalidate(Bitmap newImage) => BG3Render.Source = newImage;

		/// <summary>
		/// Sets the viewport of this Image to be passed argument
		/// </summary>
		/// <param name="viewport"></param>
		public void ResetViewports(RelativeRect viewport)
		{
			ResetViewports(viewport, viewport);
		}

		/// <summary>
		/// Sets individually the BG2 and BG3 viewports by themselves
		/// </summary>
		/// <param name="bg2Viewport"></param>
		/// <param name="bg3Viewport"></param>
		public void ResetViewports(RelativeRect bg2Viewport, RelativeRect bg3Viewport)
		{
			BG2Render.DestinationRect = bg2Viewport;
			BG3Render.DestinationRect = bg3Viewport;
		}

		/// <summary>
		/// This function should be called before rendering to the screen but after <see cref="SetContext(MAPContextDefinition?, bool, bool)"/>
		/// it cannot be used without a valid <see cref="LevelContext"/> property.
		/// <para>This function will take a ScreenSize, and optionally some screen scroll registers,
		/// and set up the view to match the given parameters and also match the mode in the <see cref="LevelContext"/></para>
		/// <para>Remember that <see cref="MAPContextDefinition.AppearancePreset"/> determines how the Background is displayed. This
		/// function will handle that for you.</para>
		/// </summary>
		/// <param name="screenBg2XScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
		/// <param name="screenBg2YScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
		/// <param name="screenBg3XScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
		/// <param name="screenBg3YScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
		/// <param name="k">The size of the *.SCR file itself</param>
		public void SetViewportsToUniformSize(double viewableWidth, double viewableHeight,
		double screenBg2XScroll = 0, double screenBg2YScroll = 0,
		double screenBg3XScroll = 0, double screenBg3YScroll = 0, int k = 1024)
		{
			if (LevelContext == null) return;
			//Most backgrounds should be bound to height of control so we don't overextend past the lower bound of the control
			var awidth = viewableHeight;

			//Converts units to screen space
			void ConvertUnits(ref double unit, double maxWidth = -1)
			{
				if (maxWidth == -1) maxWidth = awidth;
				var percentage               = unit / k;
				unit = maxWidth * percentage;
			}

			//Converts all scroll registers to screen units
			void ConvertAll(double maxWidth = -1)
			{
				ConvertUnits(ref screenBg2XScroll, maxWidth);
				ConvertUnits(ref screenBg3XScroll, maxWidth);
				ConvertUnits(ref screenBg2YScroll, maxWidth);
				ConvertUnits(ref screenBg3YScroll, maxWidth);
			}

			double nwidth = awidth;
			switch (LevelContext.AppearancePreset) {
				case "water":
				case "tunnel":
				case "undergnd":
					//Base calculations on the Width of the control
					awidth = viewableWidth;
					nwidth = awidth * 1.60;
					ConvertAll(awidth);
					ResetViewports(
						new RelativeRect(-screenBg2XScroll + ((awidth / 2) - (nwidth / 2)), screenBg2YScroll,
							(int)nwidth, (int)(awidth * 2), RelativeUnit.Absolute),
						new RelativeRect(-screenBg3XScroll, screenBg3YScroll, (int)(awidth * 2.5), (int)(awidth * 2.5),
							RelativeUnit.Absolute));
					return;
				default:
					//Base calculations on the Height of the control
					ConvertAll();
					int renderW = StarfoxEqu.RENDER_W;
					int renderH = StarfoxEqu.RENDER_W;
					int centerW = (StarfoxEqu.SCR_W / 2) - (renderW / 2);
					int centerH = (StarfoxEqu.SCR_W / 2) - (renderW / 2);
					/*
					 * ResetViewports(
						new Rect(centerW - ScreenBG2XScroll, centerH - ScreenBG2YScroll - LevelContext.ViewCY, renderW, renderH),
						new Rect(centerW - ScreenBG3XScroll, centerH - ScreenBG3YScroll, renderW, renderH));
					*/
					ResetViewports(
						new RelativeRect(-screenBg2XScroll, -screenBg2YScroll, awidth, awidth, RelativeUnit.Absolute),
						new RelativeRect(-screenBg3XScroll, -screenBg3YScroll, awidth, awidth, RelativeUnit.Absolute));
					break;
			}
		}

		public void ResizeViewports(int width, int height)
		{
			if (LevelContext == null) return;
			var bg3X = LevelContext.BG3.HorizontalOffset;
			var bg3Y = LevelContext.BG3.VerticalOffset;
			var bg2X = LevelContext.BG2.HorizontalOffset;
			var bg2Y = LevelContext.BG2.VerticalOffset;
			SetViewportsToUniformSize(width, height, bg2X, bg2Y, bg3X, bg3Y);
		}

		public override async Task SetContext(MAPContextDefinition selectedContext,
		WavyBackgroundRenderer.WavyEffectStrategies animation = WavyBackgroundRenderer.WavyEffectStrategies.None,
		bool extractCcr = false, bool extractPcr = false)
		{
			LevelContext  = selectedContext;
			AnimationMode = animation;

			//**dispose previous session
			if (bgRenderer != null) {
				bgRenderer.Dispose();
				bgRenderer = null;
			}

			ReferencedFiles.Clear();
			BG2Render.Source = null;
			BG3Render.Source = null;
			//**

			if (LevelContext == null) return;
			//Set the backgrounds for this control to update the visual
			//this also creates a bgRenderer -- which handles dynamic backgrounds
			await InvalidateBGS(extractCcr, extractPcr);
			//if animating, start the animation clock
			if (AnimationMode != WavyBackgroundRenderer.WavyEffectStrategies.None &&
				bgRenderer != null)
				StartAnimatedBackground(AnimatorEffect<object>.GetFPSTimeSpan(60));
		}

		public override void DebugInfoUpdated(AnimatorEffect<SKBitmap>.DiagnosticInfo diagnosticInformation)
		{
			base.DebugInfoUpdated(diagnosticInformation);

			TgtLatencyDebugBlock.Text = diagnosticInformation.TimerInterval.TotalMilliseconds.ToString();
			ActLatencyDebugBlock.Text = diagnosticInformation.RenderTime.TotalMilliseconds.ToString();
			BuffersBlock.Text         = diagnosticInformation.OpenBuffers.ToString();
			MemoryBlock.Text          = diagnosticInformation.MemoryUsage.ToString();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//maybe dispose here? causes too many issues though
		}

		/// <summary>
		/// Disposes of any unreleased resources
		/// </summary>
		public void Dispose()
		{
			bgRenderer?.Dispose();
		}
	}
}
