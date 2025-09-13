using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using StarFox.Interop.EFFECTS;
using StarFox.Interop.MAP.CONTEXT;
using StarwingMapVisualizer.Misc;
using StarwingMapVisualizer.Dialogs;

namespace StarwingMapVisualizer.Renderers
{
	/// <summary>
	/// An interface for controls that display Screens in Starfox to share similar components
	/// </summary>
	internal interface ISCRRendererBase
	{
		/// <summary>
		/// Describes which animation to use on this rendered background
		/// </summary>
		WavyBackgroundRenderer.WavyEffectStrategies AnimationMode { get; }

		/// <summary>
		/// Updates the <see cref="AnimationMode"/> property to the new value and, if currently
		/// rendering a dynamic background, will start using the new effect immediately.
		/// </summary>
		/// <param name="newMode"></param>
		void ChangeAnimationMode(WavyBackgroundRenderer.WavyEffectStrategies newMode);

		/// <summary>
		/// The current <see cref="MAPContextDefinition"/> this control is displaying
		/// <para/>Make sure to use the <see cref="SetContext(MAPContextDefinition?, bool, bool)"/> function
		/// </summary>
		MAPContextDefinition LevelContext { get; set; }

		/// <summary>
		/// References to any files on the disk are kept here for clarity to the end user
		/// <para/>FILE PATH -> FILE TYPE
		/// </summary>
		Dictionary<string, string> ReferencedFiles { get; }

		/// <summary>
		/// Changes the appearance of this control to the new <paramref name="selectedContext"/>
		/// </summary>
		/// <param name="selectedContext">The background preset to use</param>
		/// <param name="animation">Any animation preferred for this control</param>
		/// <param name="ExtractCCR"></param>
		/// <param name="ExtractPCR"></param>
		/// <returns></returns>
		Task SetContext(MAPContextDefinition selectedContext, WavyBackgroundRenderer.WavyEffectStrategies animation,
		bool ExtractCCR = false, bool ExtractPCR = false);
	}

	/// <summary>
	/// Has basic functionality for displaying Screens in Starfox
	/// </summary>
	public abstract class SCRRendererControlBase : ContentControl, ISCRRendererBase
	{
		protected WavyBackgroundRenderer bgRenderer;

		/// <summary>
		/// The current context being displayed by the control, if applicable
		/// </summary>
		public MAPContextDefinition LevelContext { get; set; }

		/// <summary>
		/// The files referenced by the <see cref="LevelContext"/> for clarity for the User of the program
		/// </summary>
		public Dictionary<string, string> ReferencedFiles { get; } = new Dictionary<string, string>();

		public WavyBackgroundRenderer.WavyEffectStrategies AnimationMode { get; protected set; }
		/// <summary>
		/// The target framerate to animate at. Default is <c><see cref="AnimatorEffect{T}"/>.GetFPSTimeSpan(60)</c> which is 60 FPS.
		/// <para/>This change will only be perceived once <see cref="StartAnimatedBackground(TimeSpan?)"/>
		/// has been successfully called again
		/// </summary>
		public TimeSpan TargetFrameRate { get; set; }
#if DEBUG
			= AnimatorEffect<object>.GetFPSTimeSpan(60);
#else
            = AnimatorEffect<object>.GetFPSTimeSpan(12);
#endif

		/// <summary>
		/// Causes the <see cref="SCRRendererControlBase"/> to call <see cref="BG2Invalidate(ImageSource)"/>
		/// and <see cref="BG3Invalidate(ImageSource)"/> to reflect the new <see cref="LevelContext"/>
		/// <para/>This is primarily used by <see cref="SetContext(MAPContextDefinition?, WavyBackgroundRenderer.WavyEffectStrategies, bool, bool)"/>
		/// methods to display the new <see cref="LevelContext"/> after it has been changed
		/// </summary>
		/// <param name="ExtractCCR"></param>
		/// <param name="ExtractPCR"></param>
		/// <returns></returns>
		protected async Task InvalidateBGS(bool ExtractCCR = false, bool ExtractPCR = false)
		{
			if (LevelContext == null) return;
			//RENDER BG2
			if (LevelContext.BG2ChrFile != null && LevelContext.BG2ScrFile != null) {
				ReferencedFiles.Add("Palette", LevelContext.BackgroundPalette ?? "None");
				ReferencedFiles.Add("BG2 Characters", LevelContext.BG2ChrFile);
				ReferencedFiles.Add("BG2 Screen", LevelContext.BG2ScrFile);
				try {
					//This bitmap is handed to the bgRenderer -- which will handle freeing this resource
					var source = await GFXStandard.RenderSCR(LevelContext.BackgroundPalette, LevelContext.BG2ScrFile,
						LevelContext.BG2ChrFile, -1, ExtractCCR, ExtractPCR);
					const bool diagnosticMode =
#if DEBUG
						true;
#else
                    false;
#endif
					bgRenderer = new WavyBackgroundRenderer(AnimationMode, source, diagnosticMode);
					await Dispatcher.UIThread.InvokeAsync(delegate { BG2Invalidate(source.Convert()); });
				} catch (Exception ex) {
					MessageBox.Show(ex.ToString());
				}
			}

			//RENDER BG3
			if (LevelContext.BG3ChrFile != null && LevelContext.BG3ScrFile != null) {
				ReferencedFiles.Add("BG3 Characters", LevelContext.BG3ChrFile);
				ReferencedFiles.Add("BG3 Screen", LevelContext.BG3ScrFile);
				try {
					using (var source = await GFXStandard.RenderSCR(
							   LevelContext.BackgroundPalette,
							   LevelContext.BG3ScrFile,
							   LevelContext.BG3ChrFile,
							   -1, ExtractCCR, ExtractPCR))
						await Dispatcher.UIThread.InvokeAsync(delegate { BG3Invalidate(source.Convert()); });
				} catch (Exception ex) {
					MessageBox.Show(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Starts animating the background for this <see cref="SCRRendererControlBase"/>
		/// <para>This will use <see cref="TargetFrameRate"/> to set how fast to play the animation.</para>
		/// <para/><paramref name="frameRate"/> being supplied will update the <see cref="TargetFrameRate"/> property
		/// </summary>
		/// <param name="frameRate"></param>
		/// <exception cref="NullReferenceException"></exception>
		protected void StartAnimatedBackground(TimeSpan? frameRate = null)
		{
			if (bgRenderer == null)
				throw new NullReferenceException("The background renderer has not been created.");
			TargetFrameRate = frameRate ?? TargetFrameRate;
			bgRenderer.StartAsync(
				image => { Dispatcher.UIThread.Invoke(delegate { DuringBackgroundAnimation(image); }); }, false,
				TargetFrameRate);
		}

		private void DuringBackgroundAnimation(SKBitmap image)
		{
			BG2Invalidate(image.Convert());
			image.Dispose(); // AutoDispose is off for this reason

			// ReSharper disable once PossibleNullReferenceException
			// Null check for bgRenderer done by caller
			if (bgRenderer.DiagnosticsEnabled) // checks to make sure diag info is available
				DebugInfoUpdated(bgRenderer.DiagnosticInformation);
		}

		public virtual void ChangeAnimationMode(WavyBackgroundRenderer.WavyEffectStrategies newMode)
		{
			var oldMode = AnimationMode;
			AnimationMode = newMode;
			if (oldMode == AnimationMode) return; // no change, no need to update
			if (newMode == WavyBackgroundRenderer.WavyEffectStrategies.None ||
				bgRenderer == null) // turn off animation --  or turn on animation with no bgRenderer
			{
				//reset the context to completely turn off animation or create a new bgRenderer
				_ = SetContext(LevelContext, newMode);
				return;
			}

			//bgRenderer exists
			bgRenderer.Strategy = newMode;
			if (oldMode != WavyBackgroundRenderer.WavyEffectStrategies.None)
				// hot swap change animation requires no additional code
				return;
			//turning on animation requires this being called
			StartAnimatedBackground(null); // uses targetframerate property here
		}

		/// <summary>
		/// Called when <see cref="InvalidateBGS(bool, bool)"/> has a new image to display for BG2
		/// </summary>
		/// <param name="newImage"></param>
		public abstract void BG2Invalidate(Bitmap newImage);

		/// <summary>
		/// Called when <see cref="InvalidateBGS(bool, bool)"/> has a new image to display for BG3
		/// </summary>
		/// <param name="newImage"></param>
		public abstract void BG3Invalidate(Bitmap newImage);

		public virtual void DebugInfoUpdated(WavyBackgroundRenderer.DiagnosticInfo diagnosticInformation) { }

		public abstract Task SetContext(MAPContextDefinition selectedContext,
		WavyBackgroundRenderer.WavyEffectStrategies animation, bool ExtractCCR = false, bool ExtractPCR = false);
	}
}
