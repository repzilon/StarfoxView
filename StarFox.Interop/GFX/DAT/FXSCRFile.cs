using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using StarFox.Interop.MISC;
using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.GFX.DAT
{
	/// <summary>
	/// An interface for *.SCR files, with properties for JSON serialization.
	/// </summary>
	public class FXSCRFile : SCR, IImporterObject
	{
		/// <summary>
		/// For deserialization only
		/// </summary>
		public FXSCRFile() { }

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

		[XmlIgnore]
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
			set { DeserializeTiles(0, true, nameof(Quadrant1), value); }
		}

		public string[] Quadrant2
		{
			get { return TileCodesAtQuadrant(1); }
			set { DeserializeTiles(1, false, nameof(Quadrant2), value); }
		}

		public string[] Quadrant3
		{
			get { return TileCodesAtQuadrant(2); }
			set { DeserializeTiles(2, false, nameof(Quadrant3), value); }
		}

		public string[] Quadrant4
		{
			get { return TileCodesAtQuadrant(3); }
			set { DeserializeTiles(3, false, nameof(Quadrant4), value); }
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

		private static TileMapping[] AsTileMappingArray(bool throwIfEmpty, string paramName, string[] value)
		{
			TileMapping[] tlmarInter;
			if ((value == null) || (value.Length == 0)) {
				if (throwIfEmpty) {
					throw new ArgumentNullException(paramName);
				} else {
					tlmarInter = new TileMapping[1024];
				}
			} else if (value.Length != 1024) {
				throw new ArgumentException("There must be exactly 1024 strings in the array.", paramName);
			} else {
				tlmarInter = new TileMapping[1024];
				for (int i = 0; i < 1024; i++) {
					tlmarInter[i] = TileMapping.Parse(value[i]);
				}
			}

			return tlmarInter;
		}

		private ushort[] ToNativeArray(TileMapping[] structures)
		{
			var c = structures.Length;
			ushort[] u16arSlice = new ushort[c];
			var half = this.ColorHalf;
			var format = (byte)this.CgxFormat;
			for (int i = 0; i < c; i++) {
				u16arSlice[i] = structures[i].ToNative(half, format);
			}

			return u16arSlice;
		}

		private static byte[] ToLEByteArray(ushort[] nativeArray)
		{
			byte[] bytarPair;
			var c = nativeArray.Length;
			byte[] bytarAll = new byte[checked(c * sizeof(ushort))];
			int i;
			if (BitConverter.IsLittleEndian) {
				for (i = 0; i < c; i++) {
					bytarPair = BitConverter.GetBytes(nativeArray[i]);
					bytarAll[i * 2] = bytarPair[0];
					bytarAll[i * 2 + 1] = bytarPair[1];
				}
			} else {
				for (i = 0; i < c; i++) {
					bytarPair = BitConverter.GetBytes(nativeArray[i]);
					bytarAll[i * 2] = bytarPair[1];
					bytarAll[i * 2 + 1] = bytarPair[0];
				}
			}

			return bytarAll;
		}

		private void DeserializeTiles(int zeroBasedQuadrant, bool throwIfEmpty, string paramName, string[] value)
		{
			var tlmarInter = AsTileMappingArray(throwIfEmpty, paramName, value);
			cell[zeroBasedQuadrant] = ToLEByteArray(ToNativeArray(tlmarInter));
			clear[zeroBasedQuadrant] = tlmarInter.Select(x => x.Visible).ToArray();
		}

		public void WriteTo(Stream screenFile)
		{
			int i;
			for (i = 0; i < 4; i++) {
				screenFile.Write(cell[i], 0, 0x800);
			}

			var encAscii = Encoding.ASCII;
			screenFile.Write(encAscii.GetBytes(ext.Magic), 0, 16);
			screenFile.Write(encAscii.GetBytes(ext.Version), 0, 8);
			screenFile.Write(encAscii.GetBytes(ext.Date), 0, 8);

			for (i = 0x2020; i <= 0x2040; i++) { // padding
				screenFile.WriteByte(0);
			}

			screenFile.WriteByte(mode7 ? (byte)1 : (byte)0);
			screenFile.WriteByte(scr_mode);
			screenFile.WriteByte(chr_bank);
			screenFile.WriteByte(col_bank);
			screenFile.WriteByte(col_half);
			screenFile.WriteByte(col_cell);
			screenFile.WriteByte(unk1);
			screenFile.WriteByte(unk2);

			for (i = 0x2049; i < 0x2100; i++) { // padding
				screenFile.WriteByte(0);
			}

			// Write 512 bytes of clear data
			// A jagged or a 2D array would be more compact, but more confusing
			var lstClear = new List<byte[]>(4);
			for (i = 0; i < 4; i++) {
				lstClear.Add(Utility.ToByteStream(clear[i]));
			}

			for (i = 0; i <= 2; i += 2) {
				for (int j = 0; j <= 127; j += 4) {
					screenFile.Write(lstClear[i], j, 4);
					screenFile.Write(lstClear[i + 1], j, 4);
				}
			}
		}
	}

	/// <summary>
	/// Describes a single tile usage inside a quadrant of an SCR file
	/// </summary>
	public struct TileMapping
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

		public bool Priority { get; set; }

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
			tm.Priority = ((dat & 0x2000) != 0);
			tm.FlipX = ((dat & 0x4000) != 0);
			tm.FlipY = ((dat & 0x8000) != 0);
			tm.Visible = visible;
			return tm;
		}

		public override string ToString()
		{
			return String.Format("{0}{1}{2}{3}{4}c{5}",
				this.Visible ? 'V' : 'n', this.Priority ? "!" : "",  this.Number,
				this.FlipX ? "x" : "", this.FlipY ? "y" : "", this.PaletteBaseIndex);
		}

		public static TileMapping Create(short tileNumber, byte paletteBaseIndex,
		bool visible, bool flipX, bool flipY, bool priority)
		{
			var tm = new TileMapping();
			tm.Number = tileNumber;
			tm.PaletteBaseIndex = paletteBaseIndex;
			tm.Visible = visible;
			tm.FlipX = flipX;
			tm.FlipY = flipY;
			tm.Priority = priority;
			return tm;
		}

		public static TileMapping Parse(string representation)
		{
			const string kPattern = "([Vn])([!]?)([0-9]+)(x?)(y?)c([0-9]+)";
			if (String.IsNullOrEmpty(representation)) {
				throw new ArgumentNullException(nameof(representation));
			} else {
				var match = Regex.Match(representation, kPattern);
				if (match.Success) {
					var grps = match.Groups;
					return Create(Int16.Parse(grps[3].Value), Byte.Parse(grps[6].Value),
						grps[1].Value == "V", grps[4].Value == "x", grps[5].Value == "y",
						grps[2].Value == "!");
				} else {
					throw new FormatException("The supplied string does not follow the pattern of a TileMapping text representation.");
				}
			}
		}

		public ushort ToNative(byte scrColorHalf, byte cgxFormat)
		{
			var karOffsetsByFormat = new byte[] { 4, 16, 128 };

			var color = (byte)((this.PaletteBaseIndex - (128 * scrColorHalf)) / karOffsetsByFormat[cgxFormat]);
			var dat = (ushort)((this.Number & 0x3ff) | (color << 10));
			if (this.Priority) {
				dat |= 0x2000;
			}
			if (this.FlipX) {
				dat |= 0x4000;
			}
			if (this.FlipY) {
				dat |= 0x8000;
			}
			return dat;
		}
	}
}