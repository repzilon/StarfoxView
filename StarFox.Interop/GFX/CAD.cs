﻿//render activated? requires adding support for System.Drawing.Common which really isn't necessary at this moment
#define RENDER

using System;
#if RENDER
using System.Drawing;
#endif
using System.IO;
using System.Text;
using StarFox.Interop.MISC;

// ********************************
// THANK YOU LUIGIBLOOD!
// Using open source hcgcad
// https://github.com/LuigiBlood/hcgcad
// ********************************

namespace StarFox.Interop.GFX
{
    public class CAD
    {
        /// <summary>
        /// The available formats this tool can support
        /// </summary>
        public enum BitDepthFormats : int
        {
            BPP_2 = 0,
            BPP_4 = 1,
            BPP_8 = 2
        }

        public class Extra
        {
	        public string Magic { get; set; }   //"NAK1989 S-CG-CAD"
	        public string Version { get; set; } //"VerX.XX "
	        public string Date { get; set; }    //"YYMMDD  " / "YYMMDD F"

	        /// <summary>
            /// For deserialization only
            /// </summary>
	        public Extra() { }

	        public Extra(string ext)
            {
                Magic = ext.Substring(0, 0x10);
                Version = ext.Substring(0x10, 8);
                Date = ext.Substring(0x18, 8);
            }
        }

        /// <summary>
        /// Has variables that can be tweaked to make the library handle more specialized
        /// functionality than general usage
        /// </summary>
        public class CGXContext
        {
            /// <summary>
            /// When true, all calls to RenderTile will set the 0 index color in the palette to be
            /// <see cref="Color.Transparent"/>
            /// </summary>
            public bool HandlePaletteIndex0AsTransparent { get; set; } = false;
        }

        /// <summary>
        /// Observe that <see cref="CGX.GlobalContext"/> can be used to change this default behavior
        /// <para/> See luigiblood's project on hcgcad
        /// <para/><see href="https://github.com/LuigiBlood/hcgcad"/>
        /// </summary>
        public class CGX
        {
            protected byte[] chr;     //Graphics Data
            protected Extra ext;
            protected byte col_bank;  //Color Bank
            protected byte col_half;  //Color (high, low)
            protected byte col_cell;  //Color Cell
            protected byte[] attr;    //Attribute Data

            /// <summary>
            /// The context that this library is being used in to facilitate the individualized needs of the project
            /// </summary>
            public static CGXContext GlobalContext { get; set; } = new CGXContext();
            /// <summary>
            /// <see cref="CGXContext.HandlePaletteIndex0AsTransparent"/> on <see cref="GlobalContext"/>
            /// </summary>
            private bool pal0Transparent => GlobalContext.HandlePaletteIndex0AsTransparent;

            public CGX(byte[] dat)
            {
                //Get Format
                int fmt = 0;
                int off_hdr = 0x4000;
                if (dat.Length == 0x8500)
                {
                    off_hdr = 0x8000;
                    fmt = 1;
                }
                else if (dat.Length == 0x10100)
                {
                    off_hdr = 0x10000;
                    fmt = 2;
                }

                //Get Extra
                ext = new Extra(Encoding.ASCII.GetString(Utility.Subarray(dat, off_hdr, 0x20)));
                col_bank = dat[off_hdr + 0x21];
                col_half = dat[off_hdr + 0x22];
                col_cell = dat[off_hdr + 0x23];

                //Graphics
                chr = Utility.Subarray(dat, 0, off_hdr);
                //Attributes (2BPP & 4BPP only)
                if (fmt < 2)
                    attr = Utility.Subarray(dat, off_hdr + 0x100, 0x400);
            }

            public int GetFormat()
            {
                if (chr.Length == 0x8000)
                {
                    return 1;
                }
                else if (chr.Length == 0x10000)
                {
                    return 2;
                }
                return 0;
            }

            public int GetColorBase()
            {
                switch (GetFormat())
                {
                    case 0:
                        return ((col_half * 128) + (col_cell * 4)) % 256;
                    case 1:
                        return ((col_half * 128) + (col_cell * 16)) % 256;
                    default:
                    case 2:
                        return ((col_half * 128) + (col_cell * 128)) % 256;
                }
            }

            public int GetColorBase(int attr)
            {
                switch (GetFormat())
                {
                    case 0:
                        return ((col_half * 128) + (col_cell * 4) + (attr * 4)) % 256;
                    case 1:
                        return ((col_half * 128) + (col_cell * 16) + (attr * 16)) % 256;
                    default:
                    case 2:
                        return ((col_half * 128) + (col_cell * 128) + (attr * 128)) % 256;
                }
            }
            #if RENDER
            public Bitmap Render(COL pal, int palForce = -1, int CanvasW = 128, int CanvasH = 128 * 4)
            {
                Bitmap output = new Bitmap(CanvasW, CanvasH);
                if (CanvasW < 128) CanvasW = 128;
                bool CompatRenderMode = CanvasW == 128;
                int Columns = (CanvasW / 8);
                int column = -1;
                int row = 0;
                int fmt = GetFormat();
                for (int i = 0; i < (256 * 4); i++)
                {
                    int x = ((i % 16) * 8);
                    int y = ((i / 16) * 8);
                    if (!CompatRenderMode)
                    {
                        column++;
                        if (column >= Columns)
                        {
                            column = 0;
                            row++;
                        }
                        x = column * 8;
                        y = row * 8;
                    }
                    int s = 8;
                    int p = GetColorBase();
                    if (palForce == -1)
                    {
                        //p_b = cgx[off_hdr + 0x22];
                        if (fmt < 2)
                            p = GetColorBase(attr[i]);
                    }
                    else
                    {
                        if (fmt == 0)
                            p = palForce * 4;
                        else if (fmt == 1)
                            p = palForce * 16;
                        else //if (fmt == 2)
                            p = palForce * 128;
                    }

                    var palette = pal.GetPalette(fmt, p);
                    Bitmap tile = RenderTile(i, 8, palette);

                    using (Graphics g = Graphics.FromImage(output))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.DrawImage(tile, x, y, s, s);
                    }
                }
                return output;
            }

            public Bitmap RenderTile(int tile, int size, Color[] pal, bool xflip = false, bool yflip = false)
            {
                Bitmap output = new Bitmap(size, size);
                if (pal0Transparent)
                    pal[0] = Color.Transparent; // GlobalContext.PAL0TRANS

                for (int y = 0; y < (size / 8); y++)
                {
                    for (int x = 0; x < (size / 8); x++)
                    {
                        using (Graphics g = Graphics.FromImage(output))
                        {
                            int tilecalc = (tile + (!xflip ? x : ((size / 8) - x - 1)) + (!yflip ? y * 0x10 : ((size / 8) - y - 1) * 0x10)) % 0x400;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            switch (GetFormat())
                            {
                                case 0:
                                    g.DrawImage(GFX.Render.SNES.Tile2BPP(Utility.Subarray(chr, tilecalc * 16, 16), pal, xflip, yflip), x * 8, y * 8, 8, 8);
                                    break;
                                case 1:
                                    g.DrawImage(GFX.Render.SNES.Tile4BPP(Utility.Subarray(chr, tilecalc * 32, 32), pal, xflip, yflip), x * 8, y * 8, 8, 8);
                                    break;
                                default:
                                case 2:
                                    g.DrawImage(GFX.Render.SNES.Tile8BPP(Utility.Subarray(chr, tilecalc * 64, 64), pal, xflip, yflip), x * 8, y * 8, 8, 8);
                                    break;
                            }
                        }
                    }
                }
                return output;
            }

            internal static byte[] GetRAWCGXDataArray(Stream file, BitDepthFormats fmt = BitDepthFormats.BPP_4)
            {
                byte[] dat;
                int maxlen;
                if (fmt == 0)
                {
                    dat = new byte[0x4500];
                    maxlen = 0x4000;
                }
                else if (fmt == BitDepthFormats.BPP_4)
                {
                    dat = new byte[0x8500];
                    maxlen = 0x8000;
                }
                else //if (fmt == 2)
                {
                    dat = new byte[0x10100];
                    maxlen = 0x10000;
                }

                //Generate File
                file.Read(dat, 0, Math.Min(maxlen, (int)file.Length));
                Encoding.ASCII.GetBytes("NAK1989 S-CG-CADVer0.00 010101  ").CopyTo(dat, maxlen);
                return dat;
            }

            internal static byte[] GetROMCGXDataArray(Stream file)
            {
                //CGX
                file.Seek(0, SeekOrigin.Begin);

                //Check File Size
                if (file.Length != 0x4500 && file.Length != 0x8500 && file.Length != 0x10100)
                    return null;

                int off_hdr = 0x4000;
                if (file.Length == 0x8500)
                {
                    off_hdr = 0x8000;
                }
                else if (file.Length == 0x10100)
                {
                    off_hdr = 0x10000;
                }

                //Load File
                byte[] cgx_t = new byte[file.Length];
                file.Read(cgx_t, 0, (int)file.Length);

                //Check Footer Info
                string footer_string = Encoding.ASCII.GetString(Utility.Subarray(cgx_t, off_hdr, 0x10));
                if (!footer_string.Equals("NAK1989 S-CG-CAD"))
                    return null;
                return cgx_t;
            }

            public static CGX Load(Stream file) => new CGX(GetROMCGXDataArray(file));

            public static CGX Import(Stream file, BitDepthFormats fmt) => new CGX(GetRAWCGXDataArray(file, fmt));
#endif
        }

        public class COL
        {
            Color[] col;    //Color Palette
            Extra ext;
            byte[] unk;

            bool swap;

            public COL(byte[] dat1)
            {
                col = Render.PaletteFromByteArray(dat1);
                swap = false;
            }

            public COL(byte[] dat1, byte[] dat2)
            {
                col = Render.PaletteFromByteArray(dat1);
                ext = new Extra(Encoding.ASCII.GetString(Utility.Subarray(dat2, 0, 0x20)));
                unk = Utility.Subarray(dat2, 0x100, 0x100);
                swap = false;
            }
            /// <summary>
            /// Copies a COL from one to another but overwrites to colors to be what you provide
            /// </summary>
            /// <param name="Original"></param>
            /// <param name="OverwrittenColors"></param>
            private COL(COL Original, Color[] OverwrittenColors)
            {
                col = OverwrittenColors;
                ext = Original.ext;
                unk = Original.unk;
                swap = false;
            }

            public void SetPaletteSwap(bool v)
            {
                swap = v;
            }

            public Color[] GetPalette()
            {
                return col;
            }

            public Color[] GetPalette(int fmt, int base_id)
            {
                if (fmt == 0)   //2BPP
                    return Utility.Subarray(col, base_id + (swap ? 128 : 0), 4);
                else if (fmt == 1)  //4BPP
                    return Utility.Subarray(col, base_id + (swap ? 128 : 0), 16);
                else    //if (fmt == 2)     //8BPP
                    return Utility.Subarray(col, base_id + (swap ? 128 : 0), 256);
            }
            /// <summary>
            /// Copies an entire row to another row and returns a new palette
            /// </summary>
            /// <param name="SourcePalette"></param>
            /// <param name="SourceRow"></param>
            /// <param name="DestinationRow"></param>
            /// <returns></returns>
            public static COL TransmutateByRow(COL SourcePalette, int SourceRow, int DestinationRow)
            {
                const int ROW_LEN     = 16;
                Color[]   paletteSwap = SourcePalette.GetPalette();
                Color[]   rowData     = paletteSwap.Slice((SourceRow * 16), ((SourceRow + 1) * 16));
                rowData.CopyTo(paletteSwap, (DestinationRow * ROW_LEN));
                return new COL(SourcePalette, paletteSwap);
            }
            /// <summary>
            /// Copies an entire row to another row
            /// </summary>
            /// <param name="SourceRow"></param>
            /// <param name="DestinationRow"></param>
            public void TransmutateByRow(int SourceRow, int DestinationRow)
            {
                const int ROW_LEN     = 16;
                Color[]   paletteSwap = GetPalette();
                Color[]   rowData     = paletteSwap.Slice((SourceRow * 16), ((SourceRow + 1) * 16));
                rowData.CopyTo(paletteSwap, (DestinationRow * ROW_LEN));
                col = rowData;
            }
#if RENDER
            public Bitmap RenderPalette()
            {
                Bitmap output = new Bitmap(16 * 16, 16 * 16);
                Color[] pal = Utility.Subarray(col, (swap ? 128 : 0), 256);
                for (int i = 0; i < Math.Min(pal.Length, 256); i++)
                {
                    using (Graphics g = Graphics.FromImage(output))
                        g.FillRectangle(new SolidBrush(pal[i]), (i % 16) * 16, (i / 16) * 16, 16, 16);
                }

                return output;
            }
#endif

            public static COL Load(FileStream file)
            {
                //COL
                file.Seek(0, SeekOrigin.Begin);

                //Check File Size
                if (file.Length != 0x200 && file.Length != 0x400)
                    return null;

                byte[] paldat = new byte[512];
                file.Read(paldat, 0, 512);
                if (file.Length == 0x400)
                {
                    byte[] palftr = new byte[512];
                    file.Read(palftr, 0, 512);

                    //Check Footer Info
                    string footer_string = Encoding.ASCII.GetString(Utility.Subarray(palftr, 0, 0x10));
                    if (!footer_string.Equals("NAK1989 S-CG-CAD"))
                        return null;

                    return new COL(paldat, palftr);
                }
                else
                {
                    return new COL(paldat);
                }
                //return null;
            }

            public static COL Import(FileStream file)
            {
                file.Seek(0, SeekOrigin.Begin);

                byte[] col_t = new byte[0x200];
                file.Read(col_t, 0, Math.Min(0x200, (int)file.Length));

                return new COL(col_t);
            }
        }

        public class SCR
        {
            protected byte[][] cell;  //Screen Data (4 screens of 32x32)
            protected Extra ext;
            protected bool mode7;     //Mode 7 Flag
            protected byte scr_mode;  //Screen Mode: 0 = 8x8 Tiles, 1 = 16x16 Tiles
            protected byte chr_bank;  //CHR BANK
            protected byte col_bank;  //Color Bank
            protected byte col_half;  //Color (high, low)
            protected byte col_cell;  //Color Cell
            protected byte unk1;
            protected byte unk2;

            protected bool[][] clear; //Clear Code (4 screens of 32x32, false = invisible tile, true = visible tile)

            /// <summary>
            /// Mainly for deserialization
            /// </summary>
            protected SCR()
            {
	            cell = new byte[4][];
                clear = new bool[4][];
            }

            public SCR(byte[] dat) : this()
            {
                for (int i = 0; i < 4; i++) {
                    cell[i] = Utility.Subarray(dat, i * 0x800, 0x800);
                    byte[] tmp = new byte[0x80];
                    for (int j = 0; j < 0x80; j++) {
	                    tmp[j] = dat[0x2100 + ((i & 2) * 0x80) + ((i & 1) * 4) + (j % 4) + ((j / 4) * 8)];
#if DEBUG
                        System.Diagnostics.Trace.WriteLine(String.Format(
		                    "i={0}, j={1,3}; tmp[j] = dat[{2}]; tmp[j] = dat[0x{2:x}]; tmp[j] = {3}", i, j,
		                    0x2100 + ((i & 2) * 0x80) + ((i & 1) * 4) + (j % 4) + ((j / 4) * 8), tmp[j]));
#endif
                    }
                    clear[i] = Utility.ToBitStreamReverse(tmp, 0x400);
                }
                ext = new Extra(Encoding.ASCII.GetString(Utility.Subarray(dat, 0x2000, 0x20)));
                mode7 = dat[0x2041] != 0;
                scr_mode = dat[0x2042];
                chr_bank = dat[0x2043];
                col_bank = dat[0x2044];
                col_half = dat[0x2045];
                col_cell = dat[0x2046];
                unk1 = dat[0x2047];
                unk2 = dat[0x2048];
            }
#if RENDER
            public Bitmap Render(CGX cgx, COL col, bool allvisible = false, int Screen = -1)
            {
                //Get CGX Format
                int fmt = cgx.GetFormat();

                //Tile Size
                int t = 8 * (scr_mode + 1);

                bool renderAllScreens = Screen < 0 || Screen > 3;

                Bitmap output = default;
                if (renderAllScreens)
                    output = new Bitmap(512 * (t / 8), 512 * (t / 8));
                else
                    output = new Bitmap(512 * (t / 8)/2, 512 * (t / 8)/2);
                void RenderScreen(int s)
                {
                    //Screen Data
                    for (int i = 0; i < 0x800; i += 2)
                    {
                        int sParam = s;
                        if (!renderAllScreens) sParam = 0;
                        //X Pos
                        int x = (((sParam % 2) * (t * 32)) + (((i / 2) % 32) * t));
                        //Y Pos
                        int y = (((sParam / 2) * (t * 32)) + (((i / 2) / 32) * t));
                        //Scale
                        int z = t;

                        int p_b = col_half;

                        //Map
                        ushort dat = (ushort)(cell[s][i] | (cell[s][i + 1] << 8));
                        int tile = dat & 0x3FF;
                        int color = (dat & 0x1C00) >> 10;
                        bool xflip = ((dat & 0x4000) != 0);
                        bool yflip = ((dat & 0x8000) != 0);
                        bool visible = allvisible ? true : clear[s][i / 2];
                        if (!visible) continue;

                        Bitmap chr;
                        switch (fmt)
                        {
                            case 0: //2bit
                                chr = cgx.RenderTile(tile, z, col.GetPalette(fmt, (p_b * 128) + (color * 4)), xflip, yflip);
                                break;
                            case 1: //4bit
                                chr = cgx.RenderTile(tile, z, col.GetPalette(fmt, (p_b * 128) + (color * 16)), xflip, yflip);
                                break;
                            default:
                            case 2: //8bit
                                chr = cgx.RenderTile(tile, z, col.GetPalette(fmt, (p_b * 128) + (color * 128)), xflip, yflip);
                                break;
                        }
                        using (Graphics g = Graphics.FromImage(output))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.DrawImage(chr, x, y, z, z);
                        }
                    }
                }
                if (renderAllScreens)
                {
                    //Screen ID
                    for (int s = 0; s < 4; s++)
                    {
                        RenderScreen(s);
                    }
                }
                else RenderScreen(Screen);
                return output;
            }
#endif

            internal static byte[] GetROMSCRDataArray(Stream file)
            {
                file.Seek(0, SeekOrigin.Begin);

                //Check File Size
                if (file.Length != 0x2300)
                    return null;

                byte[] scr_t = new byte[file.Length];
                file.Read(scr_t, 0, (int)file.Length);

                //Check Footer Info
                string footer_string = Encoding.ASCII.GetString(Utility.Subarray(scr_t, 0x2000, 0x10));
                if (!footer_string.Equals("NAK1989 S-CG-CAD"))
                    return null;
                return scr_t;
            }

            public static SCR Load(Stream file)
            {
                return new SCR(GetROMSCRDataArray(file));
            }

            internal static byte[] GetRAWSCRDataArray(Stream file, byte scr_mode = 0)
            {
                byte[] dat = new byte[0x2300];
                const int maxlen = 0x2000;

                //Generate File
                file.Read(dat, 0, Math.Min(maxlen, (int)file.Length));
                Encoding.ASCII.GetBytes("NAK1989 S-CG-CADVer0.00 010101  ").CopyTo(dat, maxlen);
				dat[0x2042] = scr_mode;
                Utility.Subarray(new byte[] { 0xFF }, 0, 0x200).CopyTo(dat, 0x2100);

                return dat;
            }

            public static SCR Import(Stream file, byte scr_mode = 0)
            {
                return new SCR(GetRAWSCRDataArray(file, scr_mode));
            }
        }

        public class OBJ
        {
            struct entry
            {
                public bool disp;      //Byte 0: D0000000
                public bool size;      //Byte 0: 0000000S
                public byte group;     //Byte 1
                public sbyte y;        //Byte 2
                public sbyte x;        //Byte 3
                public bool yflip;     //Byte 4: Y0000000 00000000
                public bool xflip;     //Byte 4: 0X000000 00000000
                public byte pri;       //Byte 4: 00PP0000 00000000
                public byte color;     //Byte 4: 0000CCC0 00000000
                public ushort tile;    //Byte 4: 0000000T TTTTTTTT

                public entry(byte[] dat)
                {
                    disp = (dat[0] & 0x80) != 0;
                    size = (dat[0] & 0x01) != 0;
                    group = dat[1];
                    y = (sbyte)dat[2];
                    x = (sbyte)dat[3];
                    yflip = (dat[4] & 0x80) != 0;
                    xflip = (dat[4] & 0x40) != 0;
                    pri = (byte)((dat[4] & 0x30) >> 4);
                    color = (byte)((dat[4] & 0x0E) >> 1);
                    tile = (ushort)(((dat[4] & 0x01) << 8) | dat[5]);
                }
            }

            struct sequence
            {
                public byte duration;
                public byte frame;

                public sequence(byte d, byte f)
                {
                    duration = d;
                    frame = f;
                }
            }

            entry[][] frames;   //32 frames, 64 entries each ([32][64])
            sequence[][] sequences; //16 sequences, 16 frames each ([16][32])
            Extra ext;
            byte unk0;
            byte unk1;
            byte unk2;
            byte col_half;
            byte unk4;
            byte unk5;
            byte unk6;

            public OBJ(byte[] dat)
            {
                ext = new Extra(Encoding.ASCII.GetString(Utility.Subarray(dat, 0x3000, 0x20)));
                unk0 = dat[0x3050];
                unk1 = dat[0x3051];
                unk2 = dat[0x3052];
                col_half = dat[0x3053];
                unk4 = dat[0x3054];
                unk5 = dat[0x3055];
                unk6 = dat[0x3056];

                frames = new entry[32][];
                for (int f = 0; f < 32; f++)
                {
                    frames[f] = new entry[64];
                    for (int e = 0; e < 64; e++)
                        frames[f][e] = new entry(Utility.Subarray(dat, (0x180 * f) + (e * 6), 6));
                }

                sequences = new sequence[16][];
                for (int f = 0; f < 16; f++)
                {
                    sequences[f] = new sequence[32];
                    for (int e = 0; e < 32; e++)
                        sequences[f][e] = new sequence(dat[0x3100 + (f * 0x40) + (e * 2) + 0], dat[0x3100 + (f * 0x40) + (e * 2) + 1]);
                }
            }

            public int GetSequenceFrameAmount(int seq)
            {
                int i = 0;
                for (; i < sequences[seq].Length; i++)
                {
                    if (sequences[seq][i].duration == 0 && sequences[seq][i].frame == 0)
                        return i;
                }
                return i;
            }
            #if RENDER
            public Bitmap Render(int seq, int frame, CGX cgx, COL col, int obj_size, int cgx_bank)
            {
                int f = sequences[seq][frame].frame;

                return Render(f, cgx, col, obj_size, cgx_bank);
            }

            public Bitmap Render(int frame, CGX cgx, COL col, int obj_size, int cgx_bank)
            {
                //Tile Sizes
                int[] tilesizes = { 8, 16, 8, 32, 8, 64, 16, 32, 16, 64, 32, 64 };

                Bitmap output = new Bitmap(256, 256);

                using (Graphics g = Graphics.FromImage(output))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, 254, 1, 254)), 0, 0, 256, 256);
                }

                //Get All Frame Data at once
                for (int i = 63; i >= 0; i--)
                {
                    if (!frames[frame][i].disp)
                        continue;
                    int size = tilesizes[(obj_size * 2) + (frames[frame][i].size ? 1 : 0)];
                    byte group = frames[frame][i].group;
                    sbyte y = frames[frame][i].y;
                    sbyte x = frames[frame][i].x;

                    //Assumes Big Endian for now
                    bool yflip = frames[frame][i].yflip;
                    bool xflip = frames[frame][i].xflip;
                    byte priority = frames[frame][i].pri;
                    byte color = frames[frame][i].color;
                    int tile = frames[frame][i].tile;

                    //Get 16-color Palette
                    Color[] sprpal = col.GetPalette(1, (col_half * 128) + (color * 16));
                    //Color[] sprpal = Utility.Subarray(pal, (col_half * 128) + (color * 16), 16);
                    sprpal[0] = Color.FromArgb(0, sprpal[0].R, sprpal[0].G, sprpal[0].B); //Must be transparent
                    Bitmap chr = cgx.RenderTile((cgx_bank * 256) + tile, size, sprpal, xflip, yflip);
                    //Bitmap chr = RenderCGXTile((cgx_bank * 256) + tile, size, cgx, sprpal, xflip, yflip);

                    using (Graphics g = Graphics.FromImage(output))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.DrawImage(chr, (128 + x), (128 + y), size, size);
                    }
                }
                return output;
            }

            public bool RenderOBJAnim(int seq, CGX cgx, COL pal, byte obj_size, byte cgx_bank, out Bitmap[] frames, out int[] durations)
            {
                int amountframe = GetSequenceFrameAmount(seq);

                frames = new Bitmap[amountframe];
                durations = new int[amountframe];

                if (amountframe == 0)
                    return false;

                for (int i = 0; i < amountframe; i++)
                {
                    durations[i] = sequences[seq][i].duration;
                    frames[i] = Render(seq, i, cgx, pal, obj_size, cgx_bank);
                    //durations[i] = (obj[0x3100 + (seq * 0x40) + i * 2] * 16) / 10;
                    //frames[i] = RenderOBJ(seq, i, obj, cgx, pal, obj_size, cgx_bank);
                }

                return true;
            }
#endif

            public static OBJ Load(FileStream file)
            {
                //OBJ
                file.Seek(0, SeekOrigin.Begin);

                //Check File Size
                if (file.Length != 0x3500)
                    return null;

                byte[] obj_t = new byte[file.Length];
                file.Read(obj_t, 0, (int)file.Length);

                //Check Footer Info
                string footer_string = Encoding.ASCII.GetString(Utility.Subarray(obj_t, 0x3000, 0x10));
                if (!footer_string.Equals("NAK1989 S-CG-CAD"))
                    return null;

                return new OBJ(obj_t);
            }
        }
    }
}
