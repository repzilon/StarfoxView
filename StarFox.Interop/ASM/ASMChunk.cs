using System;
using System.Globalization;
using System.IO;
using System.Text;
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
	public abstract class ASMChunk : IFormattable
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

		public virtual string ToString(string format, IFormatProvider formatProvider)
		{
			if (String.IsNullOrEmpty(format)) {
				format = "g";
			}
			if (formatProvider == null) {
				formatProvider = CultureInfo.CurrentCulture;
			}

			var stbOutput = new StringBuilder();
			stbOutput.Append(formatProvider, format, this.ChunkType);
			stbOutput.Append("@L").Append(formatProvider, format, this.Line);
			stbOutput.Append(" [").Append(formatProvider, format, this.Position);
			stbOutput.Append("..+").Append(formatProvider, format, this.Length).Append(']');
			return stbOutput.ToString();
		}
	}

	internal static class AsmChunkFormatting
	{
		internal static StringBuilder Append(this StringBuilder buffer, IFormatProvider culture, string format,
		IFormattable value)
		{
			return buffer.Append(value.ToString(format, culture));
		}

		internal static StringBuilder AppendHeader<T>(this T what, StringBuilder stbOutput, IFormatProvider culture,
		string format) where T : ASMChunk, IASMNamedSymbol
		{
			stbOutput.AppendLine(what.Name.ToUpper(culture as CultureInfo)).Append(culture, format, what.ChunkType);
			return stbOutput.Append(" defined in ").AppendLine(Path.GetFileName(what.OriginalFileName));
		}
	}
}
