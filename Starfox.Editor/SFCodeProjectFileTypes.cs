using System;
using System.Collections.Generic;
using System.IO;

namespace Starfox.Editor
{
	public enum SFCodeProjectFileTypes
	{
		Unknown,
		Include,
		Palette,
		Assembly,
		BINFile,
		CCR,
		PCR,
		CGX,
		SCR,
		MSG,
		BRR,
		SPC,
		/// <summary>
		/// An <see cref="SFOptimizerNode"/>
		/// </summary>
		SF_EDIT_OPTIM
	}

	public static class SFCodeProjectFileExtensions
	{
		private static readonly IReadOnlyDictionary<string, SFCodeProjectFileTypes> FileTypeByExtension =
			InitExtensionToFileTypeMap();

		private static Dictionary<string, SFCodeProjectFileTypes> InitExtensionToFileTypeMap()
		{
			var dictionary = new Dictionary<string, SFCodeProjectFileTypes>(StringComparer.OrdinalIgnoreCase)
			{
				{ ".asm", SFCodeProjectFileTypes.Assembly },
				{ ".ext", SFCodeProjectFileTypes.Assembly },
				{ ".mc", SFCodeProjectFileTypes.Assembly },
				{ ".inc", SFCodeProjectFileTypes.Include },
				{ ".trn", SFCodeProjectFileTypes.Include },
				{ ".col", SFCodeProjectFileTypes.Palette },
				{ ".bin", SFCodeProjectFileTypes.BINFile },
				{ ".spc", SFCodeProjectFileTypes.SPC },
				{ ".brr", SFCodeProjectFileTypes.BRR },
				{ ".ccr", SFCodeProjectFileTypes.CCR },
				{ ".pcr", SFCodeProjectFileTypes.PCR },
				{ ".cgx", SFCodeProjectFileTypes.CGX },
				{ ".scr", SFCodeProjectFileTypes.SCR },
				{ "." + SFOptimizerNode.SF_OPTIM_Extension, SFCodeProjectFileTypes.SF_EDIT_OPTIM }
			};

			return dictionary;
		}


		/// <summary>
		/// Attempts to return what kind of file this node is
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static SFCodeProjectFileTypes GetSFFileType(string filePath)
		{
			return FileTypeByExtension.TryGetValue(Path.GetExtension(filePath), out var enuType) ? enuType : SFCodeProjectFileTypes.Unknown;
		}

		/// <summary>
		/// Attempts to return what kind of file this node is
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static SFCodeProjectFileTypes GetSFFileType(this FileInfo file) => GetSFFileType(file.FullName);

		public static bool IsPlainAssemblyOnly(this FileInfo file)
		{
			var strExt = Path.GetExtension(file.FullName);
			return strExt.Equals(".ext", StringComparison.OrdinalIgnoreCase) ||
			       strExt.Equals(".mc", StringComparison.OrdinalIgnoreCase);
		}
	}
}
