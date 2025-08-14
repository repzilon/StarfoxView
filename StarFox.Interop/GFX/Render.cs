//render activated? requires adding support for System.Drawing.Common which really isn't necessary at this moment
#define RENDER

using System;
using System.Drawing;
using SkiaSharp;
using StarFox.Interop.MISC;

// ********************************
// THANK YOU LUIGIBLOOD!
// Using open source hcgcad
// https://github.com/LuigiBlood/hcgcad
// ********************************

namespace StarFox.Interop.GFX
{
    public static class Render
    {
        public static class SNES
        {
            //SNES Tile Render Functions
#if RENDER
            public static SKBitmap Tile2BPP(byte[] dat, Color[] pal, bool xflip = false, bool yflip = false)
            {
                //SNES/GB
                SKBitmap tile = new SKBitmap(8, 8);

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int colorID = (((dat[(y * 2)] & (1 << (7 - x))) << x) >> 7)
                            | (((dat[(y * 2) + 1] & (1 << (7 - x))) << x) >> 6);

                        int xt = x;
                        if (xflip) xt = 7 - x;
                        int yt = y;
                        if (yflip) yt = 7 - y;

                        tile.SetPixel(xt, yt, SFPalette.ToSkia(pal[colorID % pal.Length]));
                    }
                }

                return tile;
            }

            public static SKBitmap Tile4BPP(byte[] dat, Color[] pal, bool xflip = false, bool yflip = false)
            {
                SKBitmap tile = new SKBitmap(8, 8);

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int colorID = (((dat[(y * 2)] & (1 << (7 - x))) << x) >> 7)
                            | (((dat[(y * 2) + 1] & (1 << (7 - x))) << x) >> 6)
                            | (((dat[(y * 2) + 16] & (1 << (7 - x))) << x) >> 5)
                            | (((dat[(y * 2) + 17] & (1 << (7 - x))) << x) >> 4);

                        int xt = x;
                        if (xflip) xt = 7 - x;
                        int yt = y;
                        if (yflip) yt = 7 - y;

                        var index = colorID % pal.Length;
                        var column = index % 16;
                        var color = pal[index];
                        if (column == 0 && index != 0)
                            color = Color.Transparent;

                        tile.SetPixel(xt, yt, SFPalette.ToSkia(color));
                    }
                }
                return tile;
            }

            public static SKBitmap Tile8BPP(byte[] dat, Color[] pal, bool xflip = false, bool yflip = false)
            {
                SKBitmap tile = new SKBitmap(8, 8);

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int colorID = (((dat[(y * 2)] & (1 << (7 - x))) << x) >> 7)
                            | (((dat[(y * 2) + 1] & (1 << (7 - x))) << x) >> 6)
                            | (((dat[(y * 2) + 16] & (1 << (7 - x))) << x) >> 5)
                            | (((dat[(y * 2) + 17] & (1 << (7 - x))) << x) >> 4)
                            | (((dat[(y * 2) + 32] & (1 << (7 - x))) << x) >> 3)
                            | (((dat[(y * 2) + 33] & (1 << (7 - x))) << x) >> 2)
                            | (((dat[(y * 2) + 48] & (1 << (7 - x))) << x) >> 1)
                            | (((dat[(y * 2) + 49] & (1 << (7 - x))) << x) >> 0);

                        int xt = x;
                        if (xflip) xt = 7 - x;
                        int yt = y;
                        if (yflip) yt = 7 - y;

                        tile.SetPixel(xt, yt, SFPalette.ToSkia(pal[colorID % pal.Length]));
                    }
                }

                return tile;
            }
            #endif
        }
        //SNES Palette
        public static Color[] PaletteFromByteArray(byte[] dat)
        {
            Color[] pal = new Color[dat.Length / 2];

            for (int i = 0; (i * 2) < dat.Length; i++)
            {
                ushort colordata = (ushort)(dat[i * 2] | (dat[(i * 2) + 1] << 8));

                int r = (int)((float)((colordata & 0x1F) / 31.0f) * 255);
                int g = (int)((float)(((colordata >> 5) & 0x1F) / 31.0f) * 255);
                int b = (int)((float)(((colordata >> 10) & 0x1F) / 31.0f) * 255);

                pal[i] = Color.FromArgb(r, g, b);
            }

            return pal;
        }
#if RENDER
        public static SKBitmap RenderPalette(Color[] pal)
        {
            SKBitmap output = new SKBitmap(16 * 16, 16 * 16);

            for (int i = 0; i < Math.Min(pal.Length, 256); i++)
            {
                using (var g = new SKCanvas(output)) {
	                g.DrawRect((i % 16) * 16, (i / 16) * 16, 16, 16,
		                new SKPaint() { Style = SKPaintStyle.Fill, Color = SFPalette.ToSkia(pal[i]) });
                }
            }

            return output;
        }

        //SNES SuperCG-CAD Renderer
        //CGX
        public static SKBitmap RenderCGX(byte[] cgx, Color[] pal, int palForce = -1)
        {
            //dat: CGX file
            //pal: color palette (imported from *.COL)
            //scale: scale int
            //palForce: Force palette index (-1 = use *.CGX)

            //Get CGX Format
            int fmt = 0;
            if (cgx.Length == 0x8500)
                fmt = 1;
            else if (cgx.Length == 0x10100)
                fmt = 2;

            //Get Footer Address
            int off_hdr = 0x4000;
            for (int i = 0; i < fmt; i++)
                off_hdr *= 2;

            //Get Version
            float ver = float.Parse(System.Text.Encoding.ASCII.GetString(cgx, off_hdr + 0x13, 4), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            int rev = int.Parse(System.Text.Encoding.ASCII.GetString(cgx, off_hdr + 0x18, 6));

            SKBitmap output = new SKBitmap(128, (128) * 4);

            for (int i = 0; i < (256 * 4); i++)
            {
                int x = ((i % 16) * 8);
                int y = ((i / 16) * 8);
                int s = 8;
                int p_b = 0;
                int p = 0;
                if (palForce == -1)
                {
                    p_b = cgx[off_hdr + 0x22];
                    if (fmt < 2)
                        p = cgx[off_hdr + 0x100 + i];
                }
                else
                {
                    p = palForce;
                }

                SKBitmap tile;
                if (fmt == 0) { //2bit
	                tile = SNES.Tile2BPP(Utility.Subarray(cgx, i * 16, 16),
		                Utility.Subarray(pal, (p_b * 128) + (p * 4), 4));
                } else if (fmt == 1) { //4bit
	                tile = SNES.Tile4BPP(Utility.Subarray(cgx, i * 32, 32),
		                Utility.Subarray(pal, (p_b * 128) + (p * 16), 16));
                } else { //8bit
	                tile = SNES.Tile8BPP(Utility.Subarray(cgx, i * 64, 64),
		                Utility.Subarray(pal, (p_b * 128) + (p * 128), 256));
                }

                using (var g = new SKCanvas(output)) {
	                g.DrawBitmap(tile, x, y, new SKPaint() { IsAntialias = true, FilterQuality = SKFilterQuality.Low });
                }
            }

            return output;
        }

		public static SKBitmap RenderCGXTile(int tile, int size, byte[] cgx, Color[] pal, bool xflip = false, bool yflip = false)
		{
			SKBitmap output = new SKBitmap(size, size);

			int fmt = 0;
			if (cgx.Length == 0x8500)
				fmt = 1;
			else if (cgx.Length == 0x10100)
				fmt = 2;

			using (var g = new SKCanvas(output)) {
				var options = new SKPaint() { IsAntialias = true, FilterQuality = SKFilterQuality.Low };
				for (int y = 0; y < (size / 8); y++) {
					for (int x = 0; x < (size / 8); x++) {
						int tilecalc = (tile + (!xflip ? x : ((size / 8) - x - 1)) + (!yflip ? y * 0x10 : ((size / 8) - y - 1) * 0x10)) % 0x400;
						if (fmt == 0) {
							g.DrawBitmap(SNES.Tile2BPP(Utility.Subarray(cgx, tilecalc * 16, 16), pal, xflip, yflip),
								x * 8, y * 8, options);
						} else if (fmt == 1) {
							g.DrawBitmap(SNES.Tile4BPP(Utility.Subarray(cgx, tilecalc * 32, 32), pal, xflip, yflip),
								x * 8, y * 8, options);
						} else {
							g.DrawBitmap(SNES.Tile8BPP(Utility.Subarray(cgx, tilecalc * 64, 64), pal, xflip, yflip),
								x * 8, y * 8, options);
						}
					}
				}
			}

			return output;
		}

		//SCR
		public static SKBitmap RenderSCR(byte[] scr, byte[] cgx, Color[] pal, bool allvisible = false)
        {
            //Get CGX Format
            int fmt = 0;
            if (cgx.Length == 0x8500)
                fmt = 1;
            else if (cgx.Length == 0x10100)
                fmt = 2;

            //Get Offset to Footer
            int off_hdr = 0x2000;

            //Get Version
            float ver = float.Parse(System.Text.Encoding.ASCII.GetString(scr, off_hdr + 0x13, 4), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            int rev = int.Parse(System.Text.Encoding.ASCII.GetString(scr, off_hdr + 0x18, 6));

            //Tile Size
            int t = 8 * (scr[off_hdr + 0x42] + 1);

            SKBitmap output = new SKBitmap(512 * (t / 8), 512 * (t / 8));

            //Screen ID
            for (int s = 0; s < 4; s++)
            {
                //Screen Data
                for (int i = 0; i < 0x800; i += 2)
                {
                    //X Pos
                    int x = (((s % 2) * (t * 32)) + (((i / 2) % 32) * t));
                    //Y Pos
                    int y = (((s / 2) * (t * 32)) + (((i / 2) / 32) * t));
                    //Scale
                    int z = t;

                    int p_b = scr[off_hdr + 0x45];

                    //Map
                    ushort dat = (ushort)(scr[(s * 0x800) + i] | (scr[(s * 0x800) + i + 1] << 8));
                    int tile = dat & 0x3FF;
                    int color = (dat & 0x1C00) >> 10;
                    bool xflip = ((dat & 0x4000) != 0);
                    bool yflip = ((dat & 0x8000) != 0);
                    bool visible = ((scr[off_hdr + 0x100 + (s * 0x80) + (((i / 2) % 32) / 8)] >> (7 - (((i / 2) % 32) % 8))) & 1) != 0;
                    if (allvisible)
                        visible = true;
                    if (!visible)
                        continue;

                    SKBitmap chr;
                    if (fmt == 0) { //2bit
	                    chr = RenderCGXTile(tile, z, cgx, Utility.Subarray(pal, (p_b * 128) + (color * 4), 4), xflip, yflip);
                    } else if (fmt == 1) { //4bit
	                    chr = RenderCGXTile(tile, z, cgx, Utility.Subarray(pal, (p_b * 128) + (color * 16), 16), xflip, yflip);
                    } else { //8bit
	                    chr = RenderCGXTile(tile, z, cgx, Utility.Subarray(pal, (p_b * 128) + ((color & 1) * 128), 256), xflip, yflip);
                    }

                    using (var g = new SKCanvas(output)) {
	                    g.DrawBitmap(chr, x, y,
		                    new SKPaint() { IsAntialias = true, FilterQuality = SKFilterQuality.Low });
                    }
                }
            }

            return output;
        }

        public static SKBitmap RenderOBJ(int seq, int frame, byte[] obj, byte[] cgx, Color[] pal, byte obj_size, byte cgx_bank)
        {
            //Get Offset to Footer
            int off_hdr = 0x3000;

            int entry = obj[off_hdr + 0x100 + (seq * 0x40) + (frame * 2) + 1];

            return RenderOBJ(entry, obj, cgx, pal, obj_size, cgx_bank);
        }

        public static SKBitmap RenderOBJ(int entry, byte[] obj, byte[] cgx, Color[] pal, byte obj_size, byte cgx_bank)
        {
            //Tile Sizes
            int[] tilesizes = { 8, 16, 8, 32, 8, 64, 16, 32, 16, 64, 32, 64 };

            //Get Offset to Footer
            int off_hdr = 0x3000;

            //Get Version
            float ver = float.Parse(System.Text.Encoding.ASCII.GetString(obj, off_hdr + 0x13, 4), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            int rev = int.Parse(System.Text.Encoding.ASCII.GetString(obj, off_hdr + 0x18, 6));

            //Get Data
            //byte obj_size = obj[off_hdr + 0x50];
            byte pal_half = obj[off_hdr + 0x53];
            //byte cgx_bank = obj[off_hdr + 0x56];

            SKBitmap output = new SKBitmap(256, 256);

            using (var g = new SKCanvas(output)) {
	            g.DrawRect(0, 0, 256, 256,
		            new SKPaint() { IsAntialias = true, Style = SKPaintStyle.Fill, Color = new SKColor(254, 1, 254, 0) });
            }

            //Get All Frame Data at once
            for (int i = 63; i >= 0; i--)
            {
                bool visible = (obj[(entry * 0x180) + (i * 6) + 0] & 0x80) != 0;
                if (!visible)
                    continue;
                bool sizetype = (obj[(entry * 0x180) + (i * 6) + 0] & 0x01) != 0;
                int size = tilesizes[(obj_size * 2) + (sizetype ? 1 : 0)];
                byte group = obj[(entry * 0x180) + (i * 6) + 1];
                sbyte y = (sbyte)obj[(entry * 0x180) + (i * 6) + 2];
                sbyte x = (sbyte)obj[(entry * 0x180) + (i * 6) + 3];

                //Assumes Big Endian for now
                bool yflip = (obj[(entry * 0x180) + (i * 6) + 4] & 0x80) != 0;
                bool xflip = (obj[(entry * 0x180) + (i * 6) + 4] & 0x40) != 0;
                byte priority = (byte)((obj[(entry * 0x180) + (i * 6) + 4] & 0x30) >> 8);
                byte color = (byte)((obj[(entry * 0x180) + (i * 6) + 4] & 0x0E) >> 1);
                int tile = ((obj[(entry * 0x180) + (i * 6) + 4] & 0x01) << 8) | obj[(entry * 0x180) + (i * 6) + 5];

                //Get 16-color Palette
                Color[] sprpal = Utility.Subarray(pal, (pal_half * 128) + (color * 16), 16);
                sprpal[0] = Color.FromArgb(0, sprpal[0].R, sprpal[0].G, sprpal[0].B); //Must be transparent
                SKBitmap chr = RenderCGXTile((cgx_bank * 256) + tile, size, cgx, sprpal, xflip, yflip);

                using (var g = new SKCanvas(output)) {
	                g.DrawBitmap(chr, (128 + x), (128 + y),
		                new SKPaint() { IsAntialias = true, FilterQuality = SKFilterQuality.Low });
                }
            }
            return output;
        }

        public static bool RenderOBJAnim(int seq, byte[] obj, byte[] cgx, Color[] pal, byte obj_size, byte cgx_bank, out SKBitmap[] frames, out int[] durations)
        {
            int amountframe = 0;
            for (int i = 0; i < 0x40; i += 2)
            {
                if ((obj[0x3100 + (seq * 0x40) + i] == 0) && (obj[0x3100 + (seq * 0x40) + i + 1] == 0))
                {
                    amountframe = (i / 2) - 1;
                    break;
                }
            }

            frames = new SKBitmap[amountframe];
            durations = new int[amountframe];

            if (amountframe == 0)
                return false;

            for (int i = 0; i < amountframe; i++)
            {
                durations[i] = (obj[0x3100 + (seq * 0x40) + i * 2] * 16) / 10;
                frames[i] = RenderOBJ(seq, i, obj, cgx, pal, obj_size, cgx_bank);
            }

            return true;
        }

        public static SKBitmap ScaleBitmap(SKBitmap input, int scale)
        {
            SKBitmap output = new SKBitmap(input.Width * scale, input.Height * scale);

            using (var g = new SKCanvas(output)) {
                var rctSource = new SKRect(0, 0, input.Width, input.Height);
                var rctDest = new SKRect(0, 0, input.Width * scale, input.Height * scale);
                g.DrawBitmap(input, rctSource, rctDest,
	                new SKPaint() { IsAntialias = true, FilterQuality = SKFilterQuality.Low });
            }

            return output;
        }
#endif
    }
}
