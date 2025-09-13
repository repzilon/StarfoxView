#define IMAGING_AUTODISCARDBUFFER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace StarFox.Interop.EFFECTS
{
	/// <summary>
	/// Statuses for a <see cref="AnimatorEffect{T}"/>
	/// </summary>
	public enum AnimatorStatus
	{
		/// <summary>
		/// Not initialized yet
		/// </summary>
		NOT_INIT,

		/// <summary>
		/// Ready to begin animating
		/// </summary>
		READY,

		/// <summary>
		/// Currently running
		/// </summary>
		ANIMATING,

		/// <summary>
		/// Paused, not animating
		/// </summary>
		PAUSED,

		/// <summary>
		/// Currently being disposed
		/// </summary>
		DISPOSING,

		/// <summary>
		/// Disposed -- cannot be used again.
		/// </summary>
		DISPOSED,

		/// <summary>
		/// Had an internal error and has been stopped.
		/// </summary>
		FAULTED,
	}

	/// <summary>
	/// An abstract class that facilitates shared functionality for creating basic animations
	/// using a <see cref="Timer"/>
	/// <para/>This also has functionality for manipulating <see cref="Bitmap"/>s at high speed
	/// using caching and marshalling
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class AnimatorEffect<T> : IDisposable where T : class
	{
		protected TimeSpan animationTime = TimeSpan.Zero;
		protected System.Threading.Timer animationTimer;
		protected TimeSpan animationInterval = TimeSpan.Zero;

		/// <summary>
		/// The current status of this <see cref="AnimatorEffect{T}"/>
		/// </summary>
		public AnimatorStatus AnimatorStatus { get; protected set; } = AnimatorStatus.NOT_INIT;

		/// <summary>
		/// Information about how the <see cref="AnimatorEffect{T}"/> is doing
		/// in terms of resources and performance
		/// <para/>Note: DiagnosticEnabled must be true in the <see cref="AnimatorEffect{T}"/> <see langword="constructor"/>
		/// </summary>
		public class DiagnosticInfo
		{
			private AnimatorEffect<T> parent;

			public DiagnosticInfo(AnimatorEffect<T> parent)
			{
				this.parent = parent;
			}

			public TimeSpan TimerInterval => parent.animationInterval;
			public TimeSpan RenderTime { get; set; }
			public int OpenBuffers => parent.buffers.Count;

			public long MemoryUsage =>
				parent.buffers.Values.Select(x => x.GetUpperBound(0) * (long)x.GetUpperBound(1)).Sum() * sizeof(byte) *
				4;
		}

		/// <summary>
		/// Information about how the <see cref="AnimatorEffect{T}"/> is doing
		/// in terms of resources and performance
		/// <para/>Note: DiagnosticEnabled must be true in the <see cref="AnimatorEffect{T}"/> <see langword="constructor"/>
		/// </summary>
		public DiagnosticInfo DiagnosticInformation { get; }

		/// <summary>
		/// Note: DiagnosticEnabled must be true in the <see cref="AnimatorEffect{T}"/> <see langword="constructor"/>
		/// </summary>
		public bool DiagnosticsEnabled => DiagnosticInformation != null;

		/// <summary>
		/// The default value for this is 12 FPS. See: <see cref="GetFPSTimeSpan(double)"/>
		/// </summary>
		protected virtual TimeSpan DefaultFrameRate => GetFPSTimeSpan(12.0);

		public static TimeSpan GetFPSTimeSpan(double FramesPerSecond) =>
			TimeSpan.FromMilliseconds(1000 / Math.Max(1, FramesPerSecond));

		/// <summary>
		/// Gets whether playback of the animation is ongoing
		/// </summary>
		public bool IsAnimating => AnimatorStatus == AnimatorStatus.ANIMATING;

		/// <summary>
		/// Gets whether playback of the animation is enabled
		/// </summary>
		public bool IsPaused => AnimatorStatus == AnimatorStatus.PAUSED;

		/// <summary>
		/// True when the object has been disposed: <see cref="Dispose"/>
		/// </summary>
		public bool IsDisposed => AnimatorStatus == AnimatorStatus.DISPOSED;

		/// <summary>
		/// True when the <see cref="Dispose"/> method is currently running
		/// </summary>
		public bool Disposing => AnimatorStatus == AnimatorStatus.DISPOSING;

		/// <summary>
		/// Creates a new <see cref="AnimatorEffect{T}"/>
		/// <para/>This will set the <see cref="AnimatorStatus"/> to READY
		/// </summary>
		/// <param name="DiagnosticsEnabled">Dictates whether <see cref="DiagnosticsEnabled"/> is on or not</param>
		protected AnimatorEffect(bool DiagnosticsEnabled = false)
		{
			if (DiagnosticsEnabled)
				DiagnosticInformation = new DiagnosticInfo(this);
			AnimatorStatus = AnimatorStatus.READY;
		}

		/// <summary>
		/// Renders a new frame of the background animation.
		/// <para/>Advances this object's internal clock by the given <paramref name="DeltaTime"/>
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidDataException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		public abstract T RenderOnce(TimeSpan DeltaTime);

		/// <summary>
		/// Starts the effect. Use <see cref="Pause"/> to pause the effect.
		/// To stop the effect, dispose of this object.
		/// </summary>
		/// <param name="DueTime">The amount of time to wait until rendering the second frame. Default is 2 seconds. </param>
		/// <param name="Interval">The amount of time to wait in between rendering frames.
		/// Default is <see cref="DefaultFrameRate"/>. See: <see cref="GetFPSTimeSpan(double)"/></param>
		/// <param name="Ready">Use the <paramref name="Ready"/> callback to receive the new render output.
		/// This <see cref="Bitmap"/> will automatically be disposed after successful invokation of the callback according to
		/// <paramref name="AutoDispose"/></param>
		public void StartAsync(Action<T> Ready, bool AutoDispose = true, TimeSpan? Interval = null,
		TimeSpan? DueTime = null)
		{
			if (IsDisposed)
				throw new ObjectDisposedException(
					$"{GetType().Name} instance has been disposed, yet is now trying to be used.");
			if (animationTimer != null)
				throw new InvalidOperationException(
					"This object is already playing a background animation. You cannot call StartAsync() on it once started.");
			if (AnimatorStatus != AnimatorStatus.READY)
				throw new InvalidOperationException("The status of this object is currently: " + AnimatorStatus +
													" when it should be: READY");

			var interval = animationInterval = Interval ?? DefaultFrameRate; // 12Fps
			var dueTime  = DueTime ?? TimeSpan.FromSeconds(2);               //2secs

			AnimatorStatus = AnimatorStatus.ANIMATING;

			T render1 = RenderOnce(TimeSpan.Zero);
			Ready(render1);
			if (AutoDispose) SafeDisposeObject(ref render1);

			Stopwatch renderTime = new Stopwatch();

			animationTimer = new System.Threading.Timer(delegate {
				if (IsPaused) return;  // Paused
				if (Disposing) return; // currently disposing

				renderTime.Restart();
				T render2 = RenderOnce(interval);
				renderTime.Stop();
				if (DiagnosticsEnabled)
					DiagnosticInformation.RenderTime = renderTime.Elapsed;

				Ready(render2);
				if (AutoDispose) SafeDisposeObject(ref render2);
			}, null, dueTime, interval);
		}

		/// <summary>
		/// Disposes of the passed <see cref="T"/> object if it implements <see cref="IDisposable"/>
		/// and sets the reference to it to be null
		/// </summary>
		/// <param name="Disposable"></param>
		protected void SafeDisposeObject(ref T Disposable)
		{
			if (Disposable == null) return;
			if (Disposable is IDisposable disposable)
				disposable.Dispose();
			Disposable = null;
		}

		/// <summary>
		/// Transitions from a <see cref="AnimatorStatus"/> of ANIMATING to PAUSED
		/// </summary>
		/// <exception cref="InvalidOperationException">If the status is not ANIMATING</exception>
		public void Pause()
		{
			if (AnimatorStatus == AnimatorStatus.ANIMATING)
				AnimatorStatus = AnimatorStatus.PAUSED;
			else throw new InvalidOperationException("Tried to pause an animator that is: " + AnimatorStatus);
		}

		/// <summary>
		/// Transitions from a <see cref="AnimatorStatus"/> of PAUSED to ANIMATING
		/// </summary>
		/// <exception cref="InvalidOperationException">If the status is not PAUSED</exception>
		public void Resume()
		{
			if (AnimatorStatus == AnimatorStatus.PAUSED)
				AnimatorStatus = AnimatorStatus.ANIMATING;
			else throw new InvalidOperationException("Tried to pause an animator that is: " + AnimatorStatus);
		}

		protected abstract bool OnDispose();

		public void Dispose()
		{
			if (IsDisposed) return;
			if (animationTimer != null)
				lock (animationTimer)
					animationTimer.Dispose();
			if (!OnDispose()) return;
			GC.SuppressFinalize(this);
			AnimatorStatus = AnimatorStatus.DISPOSED;
		}

		//**HELPER FUNCTIONS

		//**BUFFER
		ConcurrentDictionary<long, Color[,]> buffers = new ConcurrentDictionary<long, Color[,]>();

		/// <summary>
		/// Use this to see if your <paramref name="Ticket"/> is valid for subsequent calls to <see cref="SetPixel(long, int, int, Color)"/>
		/// or <see cref="GetPixel(long, int, int)"/>
		/// </summary>
		/// <param name="Ticket"></param>
		/// <returns></returns>
		bool BufferTicketExists(long Ticket) => buffers.TryGetValue(Ticket, out _);

		/// <summary>
		/// Creates a new <see cref="Bitmap"/> in memory to draw to,
		/// to create animated effects.
		/// <para/>This is a memory bitmap with fast read/write functionality.
		/// </summary>
		protected long CreateBuffer(int Width, int Height)
		{
			long ticket = DateTime.Now.Ticks;
			buffers.TryAdd(ticket, new Color[Width, Height]);
			return ticket;
		}

		/// <summary>
		/// Renders the <see cref="CreateBuffer(int, int)"/> out to
		/// a <see cref="Bitmap"/> then discards the buffer.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected SKBitmap CompleteBuffer(long Ticket)
		{
			if (!BufferTicketExists(Ticket))
				throw new InvalidOperationException("You don't have a buffer created yet. Please" +
													" create a buffer first before completing it.");
			var bmp = PutPixels(buffers[Ticket]);
			DiscardBuffer(Ticket);
			return bmp;
		}

		/// <summary>
		/// Discards the current buffer. See: <see cref="CreateBuffer(int, int)"/>
		/// </summary>
		protected bool DiscardBuffer(long Ticket)
		{
#if NETFRAMEWORK || NETSTANDARD
			return buffers.TryRemove(Ticket, out _);
#else
			return buffers.Remove(Ticket, out _);
#endif
		}

		/// <summary>
		/// Gets a Pixel color at a specified position on the buffer indicated by <paramref name="Ticket"/>
		/// </summary>
		/// <param name="Ticket"></param>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected Color GetPixel(long Ticket, int X, int Y)
		{
			if (!BufferTicketExists(Ticket))
				throw new InvalidOperationException("You don't have a buffer created yet. Please" +
													" create a buffer first before completing it.");
			return buffers[Ticket][X, Y];
		}

		/// <summary>
		/// Sets a Pixel color at a specified position on the buffer indicated by <paramref name="Ticket"/>
		/// <para/>See: <see cref="CompleteBuffer(long)"/> when ready to render out your effect
		/// <para/>This method is much faster than using <see cref="Bitmap.SetPixel(int, int, Color)"/>
		/// as this approach can be batched before rendering the final product
		/// </summary>
		protected void SetPixel(long Ticket, int X, int Y, Color Color)
		{
			if (!BufferTicketExists(Ticket))
				throw new InvalidOperationException("You don't have a buffer created yet. Please" +
													" create a buffer first before completing it.");
			buffers[Ticket][X, Y] = Color;
		}

		/// <summary>
		/// Loads a <see cref="SKBitmap"/> into a <c><see cref="Color"/>[,]</c>
		/// </summary>
		/// <returns></returns>
		protected Color[,] CopyPixels(SKBitmap image)
		{
			IntPtr nintArrayLength;
			IntPtr arrPtr    = image.GetPixels(out nintArrayLength);
			byte[] rgbValues = new byte[(int)nintArrayLength];
			// Copy the RGB values into the array.
			Marshal.Copy(arrPtr, rgbValues, 0, (int)nintArrayLength);

			// TODO : Put back support for indexed color images, but in separate project because
			// Alphabloat dropped support for them in Skia
			//var colorType  = image.ColorType;
			//var octetPerPx = (colorType == SKColorType.Index8) ? (byte)1 : (byte)4;
			const byte octetPerPx = 4;

			if ((int)nintArrayLength % octetPerPx != 0) {
				throw new InvalidDataException("Bitmap data needs to be evenly divisible by " + octetPerPx + ".");
			}

			var w = image.Width;
			int i, x = 0, y = 0;

			var imageData = new Color[w, image.Height];
			/*
			if (octetPerPx == 1) {
				var palette = image.ColorTable.UnPreMultipledColors;
				for (i = 0; i < rgbValues.Length; i++) {
					byte colorByte = rgbValues[i];
					SetPixel(imageData, ToGdiPlus(palette[(byte)(colorByte >> 4)]), ref x, ref y, w);
					SetPixel(imageData, ToGdiPlus(palette[(byte)(colorByte & 0x0F)]), ref x, ref y, w);
				}
			} else {// */
				for (i = 0; i < rgbValues.Length; i += octetPerPx) {
					SetPixel(imageData,
						Color.FromArgb(rgbValues[i + 3], rgbValues[i + 2], rgbValues[i + 1], rgbValues[i]),
						ref x, ref y, w);
				}
			//}

			return imageData;
		}

		private static void SetPixel(Color[,] imageData, Color pixel, ref int x, ref int y, int w)
		{
			imageData[x, y] = pixel;
			x++;
			if (x >= w) {
				y++;
				x = 0;
			}
		}

		private static Color ToGdiPlus(SKColor skiaColor)
		{
			return Color.FromArgb(skiaColor.Alpha, skiaColor.Red, skiaColor.Green, skiaColor.Blue);
		}

		/// <summary>
		/// Creates a new <see cref="SKBitmap"/> from a <c><see cref="Color"/>[,]</c>
		/// </summary>
		/// <returns></returns>
		private static SKBitmap PutPixels(Color[,] imageData, int width = -1, int height = -1,
		Rectangle? sourceRect = null)
		{
			if (width == -1) width   = imageData.GetUpperBound(0);
			if (height == -1) height = imageData.GetUpperBound(1);
			sourceRect = sourceRect ?? new Rectangle(0, 0, width, height);

			var renderOut = new SKBitmap();
			var rgbValues = new byte[width * height * 4];
			var gcHandle  = GCHandle.Alloc(rgbValues, GCHandleType.Pinned);
			var info      = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
			var i         = 0;
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					var px = imageData[x, y];
					rgbValues[i] = px.B; i++;
					rgbValues[i] = px.G; i++;
					rgbValues[i] = px.R; i++;
					rgbValues[i] = px.A; i++;
				}
			}

			renderOut.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes,
				delegate { gcHandle.Free(); }, null);
			return renderOut;
		}
	}
}
