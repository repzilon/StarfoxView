using System.Collections.Generic;
using StarFox.Interop.ASM;

namespace StarFox.Interop.MSG
{
	/// <summary>
	/// There have been three versions of the message macro in UltraStarFox source
	/// </summary>
	public enum MessageFileVersion : byte
	{
		Unknown = 0,
		/// <summary>
		///  message	MACRO	[person,english,japanese,sound]
		/// </summary>
		EnglishAsideNoColor_01 = 1,
		/// <summary>
		/// message	MACRO	[colour,person,english,japanese,sound]
		/// </summary>
		ColoredEnglishAside_02 = 2,
		/// <summary>
		/// message	MACRO	[colour,person,text,sound]
		/// </summary>
		ColoredEnglishSeparate_03 = 3
	}

	/// <summary>
	/// Contains <see cref="MSGEntry"/> objects that represent the messages contained within the provided file
	/// </summary>
	public class MSGFile : ASMFile
	{
		/// <summary>
		/// Creates a blank <see cref="MSGFile"/> from with the given file path.
		/// </summary>
		/// <param name="FilePath"></param>
		public MSGFile(string FilePath) : base(FilePath)
		{

		}

		/// <summary>
		/// Clones from an existing <see cref="ASMFile"/> instance
		/// </summary>
		/// <param name="Base"></param>
		public MSGFile(ASMFile Base) : base(Base)
		{

		}

		/// <summary>
		/// The message entries in this file
		/// </summary>
		public Dictionary<int, MSGEntry> Entries { get; } = new Dictionary<int, MSGEntry>();

		public MessageFileVersion Version { get; private set; }

		public void FlagAs(MessageFileVersion newVersion)
		{
			if ((newVersion >= MessageFileVersion.EnglishAsideNoColor_01) && (newVersion <= MessageFileVersion.ColoredEnglishSeparate_03) && (newVersion != this.Version)) {
				this.Version = newVersion;
			}
		}
	}
}
