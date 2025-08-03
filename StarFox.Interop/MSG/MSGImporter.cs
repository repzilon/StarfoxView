using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.MISC;

namespace StarFox.Interop.MSG
{
	/// <summary>
	/// Imports MSG files into the game so the commentary can be interacted with
	/// </summary>
	public class MSGImporter : BasicCodeImporter<MSGFile>
	{
		public override string[] ExpectedIncludes { get; } =
		{
			"GAMETEXT.ASM"
		};

		public TRNFile TranslationTable { get; set; }
		public bool NeedsCharacterCodingTranslation { get; private set; }
		private static MSGFile EnglishExternalFile { get; set; }

		/// <summary>
		/// Imports the given file into a <see cref="MSGFile"/> and returns the result
		/// </summary>
		/// <param name="FilePath"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public override async Task<MSGFile> ImportAsync(string FilePath)
		{
			const string kCompatibleMacroName = "message";

			var baseFile = await BaseImportAsync(FilePath);
			var fileName = Path.GetFileName(FilePath);
			if (baseFile == null) {
				throw new InvalidOperationException(fileName + " could not be processed due to an internal error.");
			}

			// There have been three versions of the message macro in UltraStarFox source
			// 1: message	MACRO	[person,english,japanese,sound]
			// 2: message	MACRO	[colour,person,english,japanese,sound]
			// 3: message	MACRO	[colour,person,text,sound]

			bool isEnglishFile = fileName.Equals("english.inc", StringComparison.OrdinalIgnoreCase);

			var msgFile = new MSGFile(baseFile);
			int n = 0;
			foreach (var macroLine in msgFile.MacroInvokeLines) {
				if (String.Equals(macroLine.MacroReference.Name, kCompatibleMacroName, StringComparison.OrdinalIgnoreCase)) {
					var person = macroLine.TryGetParameter(0)?.ParameterContent ?? "nobody";
					MSGEntry entry;
					n++;
					if (Utility.AnyOf(person, "white", "yellow", "red", "blue")) { // person is in fact color
						person = macroLine.TryGetParameter(1)?.ParameterContent ?? "nobody";
						if (macroLine.Parameters.Length == 5) {
							entry = Create(fileName, macroLine, person, true, 2, 3, 4, n);
							msgFile.FlagAs(MessageFileVersion.ColoredEnglishAside_02);
						} else {
							entry = Create(fileName, macroLine, person, isEnglishFile, 2, 2, 3, n);
							msgFile.FlagAs(MessageFileVersion.ColoredEnglishSeparate_03);
						}
					} else {
						entry = Create(fileName, macroLine, person, true, 1, 2, 3, n);
						msgFile.FlagAs(MessageFileVersion.EnglishAsideNoColor_01);
					}
					msgFile.Entries.Add(n, entry);
				}
			}

			if (isEnglishFile) {
				EnglishExternalFile = msgFile;
			}

			return msgFile;
		}

		private MSGEntry Create(string fileName, ASMMacroInvokeLineStructure macroLine, string person, bool embeddedEnglish,
		int englishIndex, int translationIndex, int soundIndex, int rowIndex)
		{
			var english = "unknown in english";
			if (embeddedEnglish) {
				english = macroLine.TryGetParameter(englishIndex)?.ParameterContent ?? "blank in english";
			} else if (EnglishExternalFile != null) {
				english = EnglishExternalFile.Entries[rowIndex].English;
			}

			var second = macroLine.TryGetParameter(translationIndex)?.ParameterContent ?? "blank in " + fileName;
			var sound = macroLine.TryGetParameter(soundIndex)?.ParameterContent ?? "other";
			var tt = this.TranslationTable;

			if (MojiZeroTranslator.IsUtf8ReadInLatin1(second)) {
				// Upstream UltraStarFox 1 version > 2.2 as of August 2025 switched to UTF-8 without BOM and without header
#if NETFRAMEWORK || NETSTANDARD
				var charSet = Encoding.Default;
#else
				var charSet = Encoding.Latin1;
#endif
				second = Encoding.UTF8.GetString(charSet.GetBytes(second));
			} else if (MojiZeroTranslator.IsMojibake(second.Trim()) && (tt != null)) {
				// Older versions of the source (upstream or fork)
				second = MojiZeroTranslator.Decode(second, tt);
				this.NeedsCharacterCodingTranslation = true;
			} // Repzilon's UltraStarFox fork uses a charset header comment, whether the file is encoded in ISO-8859-15 (yes, part fifteen) or UTF-8; see ASMImporter.ProcChunk

			// From any source, merge dakutens in japanese text
			if (MojiZeroTranslator.IsJapaneseText(second)) {
				second = MojiZeroTranslator.MergeDakutens(second);
			}

			return new MSGEntry(person, english, second, sound);
		}
	}
}
