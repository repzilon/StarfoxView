﻿using System;
using System.IO;
using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP
{
	/// <summary>
	/// A comment in an <see cref="ASMFile"/>
	/// </summary>
	public class ASMComment : ASMChunk
	{
		/// <summary>
		/// The content of this comment
		/// </summary>
		public string CommentText { get; private set; }

		public ASMComment(string FileName, long Position)
		{
			OriginalFileName = FileName;
			this.Position = Position;
			Length = 0;
		}

		public ASMComment(string fileName, long position, int line) : this(fileName, position)
		{
			this.Line = line;
		}

		public override ASMChunks ChunkType => ASMChunks.Comment;

		/// <summary>
		/// Parses
		/// </summary>
		/// <param name="FileStream"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public override void Parse(StreamReader FileStream)
		{
			InitStream(FileStream); // move stream position
			var commentLine = FileStream.ReadLine().RemoveEscapes(); // read the line
			Length = commentLine.Length; // before making changes document the length
			commentLine = commentLine.Replace(';', ' ').Trim(); // replace the ';' with spaces and trim whitespace
			while (commentLine.Contains("  ")) // recursive remove unnecessary spaces
				commentLine = commentLine.Replace("  ", " ");
			CommentText = commentLine;
		}

		public override string ToString()
		{
			return base.ToString() + ": " + this.CommentText;
		}
	}
}
