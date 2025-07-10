using System;
using System.ComponentModel;
using System.Linq;
using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.GFX.DAT
{
	/// <summary>
	/// An interface for *.SCR files, with properties for JSON serialization.
	/// </summary>
	public class FXSCRFile : SCR, IImporterObject
	{
		/// <summary>
		/// Creates a new <see cref="FXCGXFile"/> file with the given file data.
		/// <para>To use a file path, see: <see cref="SFGFXInterface.OpenGFX(string)"/></para>
		/// </summary>
		/// <param name="dat">The file data to use as a source</param>
		/// <param name="originalFilePath"></param>
		internal FXSCRFile(byte[] dat, string originalFilePath) : base(dat)
		{
			OriginalFilePath = originalFilePath;
		}

		public string OriginalFilePath { get; }

		public bool ForMode7
		{
			get { return mode7; }
			set { mode7 = value; }
		}

		public short TileSize
		{
			get {
				if (scr_mode == 0) {
					return 8;
				} else if (scr_mode == 1) {
					return 16;
				} else { // unknown
					return -1;
				}
			}
			set {
				if (value == 8) {
					scr_mode = 0;
				} else if (value == 16) {
					scr_mode = 1;
				} else {
					throw new ArgumentOutOfRangeException(nameof(TileSize));
				}
			}
		}

		public byte CharacterBank
		{
			get { return chr_bank; }
			set { chr_bank = value; }
		}

		public byte ColorBank
		{
			get { return col_bank; }
			set { col_bank = value; }
		}

		public byte ColorHalf
		{
			get { return col_half; }
			set { col_half = value; }
		}

		public byte ColorCell
		{
			get { return col_cell; }
			set { col_cell = value; }
		}

		public Extra ScadMeta
		{
			get { return ext; }
			set { ext = value; }
		}

		private BitDepthFormats m_cgxFormat = BitDepthFormats.BPP_4;
		public BitDepthFormats CgxFormat
		{
			get { return m_cgxFormat; }
			set {
				if ((value < BitDepthFormats.BPP_2) || (value > BitDepthFormats.BPP_8)) {
					throw new InvalidEnumArgumentException(nameof(CgxFormat), (int)value, typeof(BitDepthFormats));
				} else {
					m_cgxFormat = value;
				}
			}
		}

		public string[] Quadrant1
		{
			get { return TileCodesAtQuadrant(0); }
			set {
				throw new NotImplementedException();
			}
		}

		public string[] Quadrant2
		{
			get { return TileCodesAtQuadrant(1); }
			set {
				throw new NotImplementedException();
			}
		}

		public string[] Quadrant3
		{
			get { return TileCodesAtQuadrant(2); }
			set {
				throw new NotImplementedException();
			}
		}

		public string[] Quadrant4
		{
			get { return TileCodesAtQuadrant(3); }
			set {
				throw new NotImplementedException();
			}
		}

		public TileMapping[] TilesAtQuadrant(int s)
		{
			var tlmarQuad = new TileMapping[0x800 / 2];
			var paletteHalf = col_half;
			var cgxFormat = (byte)this.CgxFormat;
			for (int i = 0; i < 0x800; i += 2) {
				ushort dat = (ushort)(cell[s][i] | (cell[s][i + 1] << 8));
				tlmarQuad[i / 2] = TileMapping.FromNative(dat, clear[s][i / 2], paletteHalf, cgxFormat);
			}
			return tlmarQuad;
		}

		private string[] TileCodesAtQuadrant(int s)
		{
			// Not using LINQ leads to less garbage to collect;
			var tlmarQuad = TilesAtQuadrant(s);
			int c = tlmarQuad.Length;
			var strarCodes = new string[c];
			for (int i = 0; i < c; i++) {
				strarCodes[i] = tlmarQuad[i].ToString();
			}
			return strarCodes;
		}
	}

	/// <summary>
	/// Describes a single tile usage inside a quadrant of an SCR file
	/// </summary>
	public class TileMapping
	{
		/// <summary>
		/// Tile number. Occupies 10 bits in native cell format.
		/// </summary>
		public short Number { get; set; }

		/// <summary>
		/// Index of the first color in sequence in the external 256 color palette.
		/// Depends on the ColorHalf property of the parent SCR file and
		/// the color depth of the associated CGX file.
		/// Occupies 3 bits in native cell format.
		/// </summary>
		public byte PaletteBaseIndex { get; set; }

		public bool FlipX { get; set; }

		public bool FlipY { get; set; }

		/// <summary>
		/// Normal visibility of the tile. Not from the cell data, but from clear data.
		/// </summary>
		public bool Visible { get; set; }

		public static TileMapping FromNative(ushort dat, bool visible, byte scrColorHalf, byte cgxFormat)
		{
			var karOffsetsByFormat = new byte[] { 4, 16, 128 };

			var tm = new TileMapping();
			tm.Number = (short)(dat & 0x3FF);
			if (cgxFormat <= 2) {
				int color = (dat & 0x1C00) >> 10;
				tm.PaletteBaseIndex = (byte)((scrColorHalf * 128) + (color * karOffsetsByFormat[cgxFormat]));
			} else {
				throw new ArgumentOutOfRangeException(nameof(cgxFormat));
			}
			tm.FlipX = ((dat & 0x4000) != 0);
			tm.FlipY = ((dat & 0x8000) != 0);
			tm.Visible = visible;
			return tm;
		}

		public override string ToString()
		{
			return String.Format("{0}{1}{2}{3}c{4}",
				this.Visible ? 'V' : 'n', this.Number,
				this.FlipX ? "x" : "", this.FlipY ? "y" : "", this.PaletteBaseIndex);
		}

		// TODO : Add Parse method
	}
}