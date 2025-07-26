using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace StarFox.Interop.MSG
{
	public class TRNImporter : BasicCodeImporter<TRNFile>
	{
		public override string[] ExpectedIncludes { get; } =
		{
			"FONTDATA.ASM"
		};

		public override async Task<TRNFile> ImportAsync(string FilePath)
		{
			const string kCompatibleMacroName = "MOJI";

			var baseFile = await BaseImportAsync(FilePath);
			var fileName = Path.GetFileName(FilePath);
			if (baseFile == null) {
				throw new InvalidOperationException(fileName + " could not be processed due to an internal error.");
			}

			var codePointToTileNumber = new TRNFile(baseFile);
			var ciInvariant = CultureInfo.InvariantCulture;
			foreach (var macroLine in codePointToTileNumber.MacroInvokeLines) {
				if (String.Equals(macroLine.MacroReference.Name, kCompatibleMacroName,
						StringComparison.OrdinalIgnoreCase)) {
					var c = macroLine.Parameters.Count;
					for (var i = 0; i < c; i++) {
						var tile = macroLine.TryGetParameter(i).Value.Trim();
						byte number = tile.StartsWith('$') ?
						 Byte.Parse(tile.Substring(1), NumberStyles.HexNumber, ciInvariant) :
						 Byte.Parse(tile, NumberStyles.None, ciInvariant);
						codePointToTileNumber.Append(number);
					}
				}
			}

			return codePointToTileNumber;
		}
	}
}
