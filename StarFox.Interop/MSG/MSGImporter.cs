using System;
using System.IO;
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
			foreach (var macroLine in msgFile.MacroInvokeLines) {
				if (String.Equals(macroLine.MacroReference.Name, kCompatibleMacroName, StringComparison.OrdinalIgnoreCase)) {
					var person = macroLine.TryGetParameter(0)?.ParameterContent ?? "nobody";
					MSGEntry entry;
					if (Utility.AnyOf(person, "white", "yellow", "red", "blue")) { // person is in fact color
						person = macroLine.TryGetParameter(1)?.ParameterContent ?? "nobody";
						if (macroLine.Parameters.Length == 5) {
							entry = Create(fileName, macroLine, person, true, 2, 3, 4);
							msgFile.FlagAs(MessageFileVersion.ColoredEnglishAside_02);
						} else {
							entry = Create(fileName, macroLine, person, isEnglishFile, 2, 2, 3);
							msgFile.FlagAs(MessageFileVersion.ColoredEnglishSeparate_03);
						}
					} else {
						entry = Create(fileName, macroLine, person, true, 1, 2, 3);
						msgFile.FlagAs(MessageFileVersion.EnglishAsideNoColor_01);
					}
					msgFile.Entries.Add(msgFile.Entries.Count + 1, entry);
				}
			}
			return msgFile;
		}

		private MSGEntry Create(string fileName, ASMMacroInvokeLineStructure macroLine, string person, bool embeddedEnglish,
		int englishIndex, int translationIndex, int soundIndex)
		{
			var english = "unknown in english";
			if (embeddedEnglish) {
				english = macroLine.TryGetParameter(englishIndex)?.ParameterContent ?? "blank in english";
			}
			var second = macroLine.TryGetParameter(translationIndex)?.ParameterContent ?? "blank in " + fileName;
			var sound = macroLine.TryGetParameter(soundIndex)?.ParameterContent ?? "other";

			var tt = this.TranslationTable;
			if (MojiZeroTranslator.IsMojibake(second) && (tt != null)) {
				second = MojiZeroTranslator.Decode(second, tt);
				this.NeedsCharacterCodingTranslation = true;
			}

			return new MSGEntry(person, english, second, sound);
		}
	}
}
