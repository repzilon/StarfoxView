using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MISC;

namespace StarFox.Interop.GFX
{
	/// <summary>
	/// An interface to interact with <see cref="GFX.DAT.FXCGXFile"/> and <see cref="GFX.DAT.FXDatFile"/> objects
	/// </summary>
	public static class SFGFXInterface
	{
		/// <summary>
		/// Opens a Starfox DAT (or BIN) file containing <see cref="FXPCRFile"/> data
		/// </summary>
		/// <returns></returns>
		//public static async Task<FXDatFile> OpenDATFile(string FilePath) => await FX.ExtractGraphics(FilePath);

		static async Task<byte[]> ExtractDecrunch(string fullName)
		{
			using (var fs = File.OpenRead(fullName)) {
				byte[] fileArray = new byte[fs.Length];
				await fs.ReadAsync(fileArray, 0, fileArray.Length);
				return Decrunch.Decompress(fullName, fileArray);
			}
		}

		/// <summary>
		/// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
		/// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
		/// </summary>
		/// <param name="fullName">The full name of the file to convert.</param>
		/// <returns></returns>
		public static async Task<string> TranslateCompressedCCR(string fullName, CAD.BitDepthFormats BitDepth = CAD.BitDepthFormats.BPP_4)
		{
			string name = Path.ChangeExtension(fullName, "CGX");
			var file = await ExtractDecrunch(fullName);
			using (var ms = new MemoryStream(file)) {
				var bytes = CAD.CGX.GetRAWCGXDataArray(ms, BitDepth);
#if NETFRAMEWORK || NETSTANDARD
				File.WriteAllBytes(name, bytes);
#else
				await File.WriteAllBytesAsync(name, bytes);
#endif
				return name;
			}
		}

		/// <summary>
		/// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
		/// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
		/// </summary>
		/// <param name="fullName">The full name of the file to convert.</param>
		/// <returns></returns>
		public static async Task<string> TranslateCompressedPCR(string fullName, int scr_mode = 0)
		{
			string name = Path.ChangeExtension(fullName, "SCR");
			var file = await ExtractDecrunch(fullName);
			//await File.WriteAllBytesAsync(name, file);
			//return;
			using (var ms = new MemoryStream(file)) {
				var bytes = CAD.SCR.GetRAWSCRDataArray(ms, 0);
#if NETFRAMEWORK || NETSTANDARD
				File.WriteAllBytes(name, bytes);
#else
				await File.WriteAllBytesAsync(name, bytes);
#endif
				return name;
			}
		}

		private static async Task<FXCGXFile> ImportCGX(string FileName, CAD.BitDepthFormats BitDepth, bool wasGuessing)
		{
			using (var fs = File.OpenRead(FileName)) {
				var baseData = CAD.CGX.GetRAWCGXDataArray(fs, BitDepth);
				return baseData == null ? null : new FXCGXFile(baseData, FileName,
				 wasGuessing ? CgxLoadingStrategy.GuessDepth : CgxLoadingStrategy.AskDepth);
			}
		}

		/// <summary>
		/// Opens a raw *.CGX file -- as in has no metadata.
		/// </summary>
		/// <param name="FileName">The path to get to the file</param>
		/// <param name="BitDepth">Color depth of the pixel data</param>
		/// <returns></returns>
		public static async Task<FXCGXFile> ImportCGX(string FileName, CAD.BitDepthFormats BitDepth)
		{
			return await ImportCGX(FileName, BitDepth, false);
		}

		/// <summary>
		/// Imports a truncated CGX, guessing its bit depth.
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="high">Minimum size for which a file is considered 4 bpp</param>
		/// <param name="low">Maximum size for which a file is considered 2 bpp</param>
		/// <param name="rawCgx"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method supports TryImportFoxCGX and should not be made public.
		/// </remarks>
		private static async Task<FXCGXFile> ImportTruncatedCGX(string filePath, int high, int low, byte[] rawCgx)
		{
			FXCGXFile fxGFX = null;
			if (rawCgx.Length > high) {
				fxGFX = await ImportCGX(filePath, CAD.BitDepthFormats.BPP_4, true);
			} else if (rawCgx.Length < low) {
				fxGFX = await ImportCGX(filePath, CAD.BitDepthFormats.BPP_2, true);
			}
			return fxGFX;
		}

		/// <summary>
		/// Tries to import a truncated CGX files found in StarFox source code, guessing
		/// its bit depth from its size.
		/// </summary>
		/// <param name="filePath">Full path to the CGX file</param>
		/// <returns></returns>
		public static async Task<FXCGXFile> TryImportFoxCGX(string filePath)
		{
			FXCGXFile fxGFX = null;
#if NETFRAMEWORK || NETSTANDARD
			var bytarCgxRaw = File.ReadAllBytes(filePath);
#else
			var bytarCgxRaw = await File.ReadAllBytesAsync(filePath);
#endif
			fxGFX = await ImportTruncatedCGX(filePath, 4096, 1056, bytarCgxRaw);

			// Even truncated by file format standard, some files have trailing NUL bytes
			if (fxGFX == null) {
				fxGFX = await ImportTruncatedCGX(filePath, 2036, 576, Utility.TrimEnd(bytarCgxRaw));
			}
			// This is getting weird, smaller remaining files have higher color depth
			if (fxGFX == null) {
				if (bytarCgxRaw.Length < 2048) {
					fxGFX = await ImportCGX(filePath, CAD.BitDepthFormats.BPP_4, true);
				} else if (bytarCgxRaw.Length > 2048) {
					fxGFX = await ImportCGX(filePath, CAD.BitDepthFormats.BPP_2, true);
				} // else length == 2048 and we cannot guess, because some are 2 bpp and others are 4 bpp.
			}

			return fxGFX;
		}

		/// <summary>
		/// Opens a well-formed *.CGX file
		/// </summary>
		/// <param name="FileName">The path to get to the file</param>
		/// <returns></returns>
		public static FXCGXFile OpenCGX(string FileName)
		{
			using (var fs = File.OpenRead(FileName)) {
				var baseData = CAD.CGX.GetROMCGXDataArray(fs);
				return baseData == null ? null : new FXCGXFile(baseData, FileName, CgxLoadingStrategy.Standard);
			}
		}

		/// <summary>
		/// Opens a well-formed *.SCR file
		/// </summary>
		/// <param name="FileName">The path to get to the file</param>
		/// <returns></returns>
		public static FXSCRFile OpenSCR(string FileName)
		{
			using (var fs = File.OpenRead(FileName)) {
				var baseData = CAD.SCR.GetROMSCRDataArray(fs);
				return baseData == null ? null : new FXSCRFile(baseData, FileName);
			}
		}

		/// <summary>
		/// Opens a raw/truncated *.SCR file
		/// </summary>
		/// <param name="FileName">The path to get to the file</param>
		/// <returns></returns>
		public static FXSCRFile ImportSCR(string FileName)
		{
			using (var fs = File.OpenRead(FileName)) {
				var baseData = CAD.SCR.GetRAWSCRDataArray(fs, 0);
				return baseData == null ? null : new FXSCRFile(baseData, FileName);
			}
		}

		/// <summary>
		/// Opens a Starfox DAT (or BIN) file containing <see cref="FXPCRFile"/> data
		/// and saves it as two *.CGX files (low and high bank).
		/// </summary>
		/// <returns></returns>
		public static async Task TranslateDATFile(string FilePath, bool SaveMSXBanks = false)
		{
			var hiLowBanks = await FX.ExtractGraphics(FilePath);
			if (SaveMSXBanks)
				await hiLowBanks.Save(FilePath);
			var datFile = new FXDatFile(hiLowBanks, FilePath);
			await datFile.Save();
		}

		/// <summary>
		/// Renders an <see cref="MSprite"/> when supplied with all MSprite graphics banks
		/// </summary>
		/// <param name="Sprite"></param>
		/// <param name="P_Col">Must be P_Col to avoid incorrect coloration</param>
		/// <param name="CGXBanks">Must be in LOW -> HIGH order.</param>
		/// <returns></returns>
		public static SKBitmap RenderMSprite(MSprite Sprite, CAD.COL P_Col, params FXCGXFile[] CGXBanks)
		{
			if (CGXBanks.Length % 2 != 0) throw new ArgumentOutOfRangeException("CGX banks provided should be High AND Low banks.");
			int bank = Sprite.Parent.BankIndex * 2 + (Sprite.HighBank ? 1 : 0);
			if (CGXBanks.Length < bank) throw new ArgumentOutOfRangeException("CGX banks provided is not enough for the supplied sprite.");
			FXCGXFile source = CGXBanks[bank];
			using (var bmp = source.Render(P_Col, -1, 256, 128)) {
				return Clip(bmp, new Rectangle(Sprite.X, Sprite.Y, Sprite.Width, Sprite.Height));
			}
		}

		private static SKBitmap Clip(SKBitmap Src, Rectangle ViewRect)
		{
			SKBitmap newBmp = new SKBitmap(ViewRect.Width, ViewRect.Height);
			Src.ExtractSubset(newBmp, new SKRectI(ViewRect.Left, ViewRect.Top, ViewRect.Right, ViewRect.Bottom));
			//newBmp.MakeTransparent(Color.Transparent);
			return newBmp;
		}

		/// <summary>
		/// Converts a .sfscreen file (a JSON representation of a .SCR file) back to its native binary format.
		/// </summary>
		/// <param name="sfscreenFilePath">Path to .sfscreen file</param>
		/// <param name="binaryFilePath">Path to the destination file</param>
		public static void ConvertSfscreenToSCR(string sfscreenFilePath, string binaryFilePath)
		{
			var fxSCR = JsonImportExport.LoadTo<FXSCRFile>(sfscreenFilePath);
			using (var fs = File.OpenWrite(binaryFilePath)) {
				fxSCR.WriteTo(fs);
			}
		}
	}
}
