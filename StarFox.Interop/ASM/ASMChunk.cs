﻿using System;
using System.IO;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM
{
	public enum ASMChunks
	{
		Unknown,
		Comment,
		Macro,
		Line,
		Constant
	}

	public interface IASMNamedSymbol
	{
		string Name { get; }
	}

	/// <summary>
	/// A block of ASM code
	/// </summary>
	public abstract class ASMChunk
	{
		/// <summary>
		/// The original file this chunk can be found in
		/// </summary>
		public string OriginalFileName { get; internal set; }
		public abstract ASMChunks ChunkType { get; }
		public long Position { get; internal set; }
		public virtual int Line { get; internal set; }
		public long Length { get; internal set; }

		/// <summary>
		/// Moves the supplied stream to the <see cref="Position"/> property's value
		/// </summary>
		internal void InitStream(StreamReader textFile)
		{
			textFile.BaseStream.Seek(Position, SeekOrigin.Begin);
			textFile.DiscardBufferedData();
		}

		public abstract void Parse(StreamReader textFile);

		/// <summary>
		/// Supplied with the first line of a chunk this function will guess what it is.
		/// </summary>
		/// <param name="ChunkHeader"></param>
		/// <returns></returns>
		public static ASMChunks Conjecture(string ChunkHeader)
		{
			if (ChunkHeader == null) throw new ArgumentNullException(nameof(ChunkHeader));
			ChunkHeader = ChunkHeader.RemoveEscapes().TrimStart(); // trim whitespace
			if (ChunkHeader.StartsWith(';')) // comment
				return ASMChunks.Comment;
			if (ASMMacro.CheckMacroHeader(ChunkHeader)) // is this a macro?
				return ASMChunks.Macro; // macro spotted
			return ASMChunks.Line; // probably a line? TODO: add more checking
		}

		public override bool Equals(object obj)
		{
			return (obj is ASMChunk chunk) && (chunk.OriginalFileName == OriginalFileName) && (chunk.Position == Position) && (chunk.Length == Length);
		}

		public override string ToString()
		{
			return $"{this.ChunkType} @L{this.Line} [{this.Position}..+{this.Length}]";
		}
	}
}
