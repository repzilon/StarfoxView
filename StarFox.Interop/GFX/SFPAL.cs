using System;
using System.Collections.Generic;
using System.Drawing;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.COLTAB.DEF;
using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.GFX
{
	/// <summary>
	/// Provides an interface to generate palettes compatible with Starfox.
	/// <para>This will handle creating colors through Lerping to emulate dithering.</para>
	/// </summary>
	public class SFPalette
	{
		public string Name { get; }

		private readonly COL palette;
		private readonly COLGroup group;

		/// <summary>
		/// The generated colors added to this <see cref="SFPalette"/>
		/// <para>Call <see cref="GetPalette"/> to evaluate this property.</para>
		/// </summary>
		public Color[] Colors { get; private set; } = { };
		/// <summary>
		/// A filtered list of just collites (light colors)
		/// </summary>
		public Dictionary<int, Color> Collites { get; } = new Dictionary<int, Color>();
		/// <summary>
		/// A filtered list of just coldepths (light colors)
		/// </summary>
		public Dictionary<int, Color> Coldepths { get; } = new Dictionary<int, Color>();
		/// <summary>
		/// A filtered list of just colnorms (colors based on normal of face ... in relation to a supposed source)
		/// </summary>
		public Dictionary<int, Color> Colnorms { get; } = new Dictionary<int, Color>();
		/// <summary>
		/// External palettes (such as animations) are listed here to include in Editors
		/// </summary>
		public HashSet<string> ReferencedPaletteNames { get; } = new HashSet<string>();
		/// <summary>
		/// External textures (such as <see cref="MSprites"/>) are listed here to include in Editors
		/// </summary>
		public HashSet<string> ReferencedTextureNames { get; } = new HashSet<string>();

		private bool IsOdd = false;
		private int colorPaletteStartIndex = -1;
		private Color GetColorByIndex(int Index)
		{
			var colors = palette.GetPalette();
			return colors[Math.Min(colors.Length - 1, Index + colorPaletteStartIndex)];
		}

		/// <summary>
		/// Create a new <see cref="SFPalette"/> taking colors from Palette and context from Group.
		/// </summary>
		/// <param name="Palette">The colors to use</param>
		/// <param name="Group">The color table found in a <see cref="COLTABFile"/></param>
		public SFPalette(string Name, in COL Palette, in COLGroup Group)
		{
			this.Name = Name;
			palette = Palette;
			group = Group;

			var colors = palette.GetPalette();
			//skip all black color except palette
			for (int i = 0; i < colors.Length; i++) {
				var color = colors[i];
				if (color.R == 0 && color.G == 0 && color.B == 0) // black
					continue;
				colorPaletteStartIndex = i - 1;
				break;
			}
			if (colorPaletteStartIndex < 1) colorPaletteStartIndex = 0;
		}

		public Bitmap RenderPalette()
		{
			double sqSize = Colors.Length < 1 ? 1 : Math.Sqrt(Colors.Length);
			int isqSize = (int)Math.Ceiling(sqSize); // round up for square size
			Bitmap bmp = new Bitmap(isqSize, isqSize);
			for (int y = 0; y < isqSize; y++) { // row
				for (int x = 0; x < isqSize; x++) { // col
					int index = (isqSize * y) + x;
					var color = index < Colors.Length ? Colors[index] : Color.White;
					bmp.SetPixel(x, y, color);
				}
			}
			return bmp;
		}

		/// <summary>
		/// Gets the palette, this is only compatible with 8BPP mode
		/// </summary>
		/// <returns></returns>
		public Color[] GetPalette()
		{
			Collites.Clear(); // clear previous colors now
			Coldepths.Clear();
			Colnorms.Clear();
			ReferencedPaletteNames.Clear();
			ReferencedTextureNames.Clear();

			IsOdd = false;
			var colors = new List<Color>();
			int i = -1, actualPosition = -1;
			foreach (var definition in group.Definitions) {
				i++;
				actualPosition++;
				//COLDefinition.CallTypes.Colsmooth => HandleColsmooth(definition as COLSmooth),
				Color? color;
				if (definition.CallType == COLDefinition.CallTypes.Collite) {
					color = HandleCollite(definition as COLLite);
				} else if (definition.CallType == COLDefinition.CallTypes.Coldepth) {
					color = HandleColdepth(definition as COLDepth);
				} else if (definition.CallType == COLDefinition.CallTypes.Colnorm) {
					color = HandleColnorm(definition as COLNorm);
				} else {
					color = null;
				}

				if (definition is COLAnimationReference anim)
					ReferencedPaletteNames.Add(anim.TableName);
				if (definition is COLTexture texture)
					ReferencedTextureNames.Add(texture.Reference);
				if (color == null) {
					i--;
					continue;
				}
				colors.Add(color.Value);
			}
			return Colors = colors.ToArray();
		}

		public static float Lerp(float a, float b, float t)
		{
			return a + (b - a) * t;
		}

		public static Color LerpColor(Color color1, Color color2, float t = .75f)
		{
			float r = Lerp(color1.R, color2.R, t);
			float g = Lerp(color1.G, color2.G, t);
			float b = Lerp(color1.B, color2.B, t);

			return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
		}
		private Color GetGreyscale(float Intensity) =>
			GetGreyscale(GetColorByIndex(0x9), Color.White, Intensity);
		private Color GetGreyscale(Color From, Color To, float Intensity) => LerpColor(From, To, Intensity);
		Color GetSolidColor(int ColorByte)
		{ // SOLID COLOR
			var dirtyByte = ColorByte - 11;
			int index = (dirtyByte / 2) + (dirtyByte % 2);
			index++;
			var thisColor = GetColorByIndex(index); // 8BPP get color by index
			return thisColor;
		}
		private Color GetMixies(int ColorByte)
		{
			Color Color1 = GetSolidColor(0x16); // get blue by default
			Color Color2 = Color.White;
			switch (ColorByte) {
				case 0x19:
					Color2 = GetColorByIndex(1); // dark red
					break;
				case 0x1a:
					Color2 = GetColorByIndex(2); // coral
					break;
				case 0x1b:
					Color2 = GetColorByIndex(3); // orange
					break;
				case 0x1c:
					Color2 = GetColorByIndex(4); // bright orange
					break;
				case 0x1d:
					Color1 = GetColorByIndex(0xF); // green
					Color2 = GetColorByIndex(0xA); // dark grey
					break;
				case 0x1f:
					Color1 = GetColorByIndex(0xF); // green
					Color2 = GetColorByIndex(0xD); // silver
					break;
			}
			return LerpColor(Color1, Color2, .5f);
		}
		private Color? HandleColdepth(COLDepth DepthColor)
		{
			var color = HandleDepthColorByte(DepthColor.ColorByte);
			if (!color.HasValue) return null;

			Coldepths.TryAdd(DepthColor.ColorByte, color.Value);

			return color;
		}
		private Color? HandleDepthColorByte(int ColorByte)
		{
			if (ColorByte < 0x0B) { // 1-10 reserved for greyscale
				var grey = GetGreyscale(ColorByte / 10.0f);
				return grey;
			}
			if (ColorByte == 0x1f || (ColorByte >= 0x19 && ColorByte < 0x1e)) // 0x19 -> 0x1d && 0x1f is mixies
				return GetMixies(ColorByte);
			if (ColorByte == 0x1e) {
				var color = GetColorByIndex(0xF);
				return color;
			}
			if (ColorByte == 0x12) {
				IsOdd = false;
			}
			bool isInbetween = IsOdd; // simple alternator
			if (isInbetween) {
				IsOdd = false;
				var previousColor = Coldepths[ColorByte - 1];
				var nextColor = GetSolidColor(ColorByte + 1);
				var thisColor = LerpColor(previousColor, nextColor, .5f); // lerp by half of both colors
				return thisColor;
			}
			IsOdd = true;
			return GetSolidColor(ColorByte);
		}
		private Color HandleColorByte(int ColorByte)
		{
			if (Collites.TryGetValue(ColorByte, out var color))
				return color;

			Color collite;
			var karFirstIndexes = new byte[10] { 10, 9, 9, 9, 9, 9, 2, 5, 2, 0xF };
			var karSecondIndexes = new byte[10] { 11, 10, 2, 5, 3, 6, 9, 9, 5, 0x9 };
			if ((ColorByte >= 0) && (ColorByte <= 9)) {
				collite = LerpColor(GetColorByIndex(karFirstIndexes[ColorByte]), GetColorByIndex(karSecondIndexes[ColorByte]));
			} else {
#if NETSTANDARD2_0 || NETCOREAPP2_1
				collite = Backporting.FromHtml("#FFFFFF");
#else
				collite = ColorTranslator.FromHtml("#FFFFFF");
			}

			if (false) {
				// Falling back on CoolK definitions until parser is complete
				var karFallback = new string[] {
					"#E7E7E7", //=Solid Dark Grey
                    "#AAAAAA", //= Solid Darker Grey
                    "#BA392A", //= Shaded Bright Red/Dark Red
                    "#5144D4", //= Shaded Blue/Bright Blue
                    "#D8A950", //= Shaded Bright Orange/Black
                    "#190646", //= Shaded Turquoise/Black
                    "#801009", //= Solid Dark Red
                    "#2411A3", //= Solid Blue
                    "#7C11A3", //= Shaded Red/blue (Purple)
                    "#2F9E28", //= Shaded Green/Dark Green
                };
#if NETSTANDARD2_0 || NETCOREAPP2_1
				collite = Backporting.FromHtml((ColorByte >= 0) && (ColorByte <= 9) ? karFallback[ColorByte] : "#FFFFFF");
#else
				collite = ColorTranslator.FromHtml((ColorByte >= 0) && (ColorByte <= 9) ? karFallback[ColorByte]: "#FFFFFF");
#endif
			}
			Collites.Add(ColorByte, collite);
			return collite;
		}
		/// <summary>
		/// Processes a <see cref="COLLite"/> color (WIP)
		/// </summary>
		/// <param name="LightColor"></param>
		/// <returns></returns>
		private Color HandleCollite(COLLite LightColor) => HandleColorByte(LightColor.ColorByte);
		/// <summary>
		/// Very WIP!
		/// </summary>
		/// <param name="LightColor"></param>
		/// <returns></returns>
		private Color HandleColnorm(COLNorm LightColor)
		{
			var color = GetColorByIndex(LightColor.ColorByte);
			Colnorms.TryAdd(LightColor.ColorByte, color);
			return color;
		}
		/// <summary>
		/// Very WIP!
		/// </summary>
		/// <param name="LightColor"></param>
		/// <returns></returns>
		private Color HandleColsmooth(COLSmooth LightColor) => HandleDepthColorByte(LightColor.ColorByte) ?? Color.Red;
	}
}
