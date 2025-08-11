using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP
{
	/// <summary>
	/// A comment in an <see cref="ASMFile"/>
	/// </summary>
	public class ASMMacro : ASMChunk, IASMNamedSymbol
	{
		private readonly ASMImporterContext context;

		/// <summary>
		/// If this macro is not a macro, or invalid this is false
		/// </summary>
		public bool IsValid { get; private set; } = false;
		public string Name { get; private set; } = "";
		public string[] Parameters { get; private set; } = new string[0];
		public ASMChunk[] Lines { get; private set; } = { };

		internal ASMMacro(long Position, ASMImporterContext Context)
		{
			OriginalFileName = Context.CurrentFilePath;
			this.Position = Position;
			Length = 0;
			context = Context;
		}

		internal ASMMacro(long position, ASMImporterContext context, int line) : this(position, context)
		{
			this.Line = line;
		}

		public override ASMChunks ChunkType => ASMChunks.Macro;
		/// <summary>
		/// Looks at the given chunk header to see if this is a Macro
		/// </summary>
		/// <param name="Header"></param>
		/// <returns></returns>
		public static bool CheckMacroHeader(string Header)
		{
			var headerLine = Header;
			headerLine = headerLine.NormalizeFormatting(true);
			var blocks = headerLine.Split(' ');
			if (blocks.Length < 2) // not valid formatting
				return false;
			return blocks[1].ToLower() == "macro";
		}

		/// <summary>
		/// Parses
		/// </summary>
		/// <param name="FileStream"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public override void Parse(StreamReader FileStream)
		{
			void fail()
			{
				IsValid = false;
			}
			InitStream(FileStream); // move stream position
			var headerLine = FileStream.ReadLine(); // read the line
			long newPosition = FileStream.GetActualPosition();
			long runningLength = newPosition - Position; // start tracking the length of this block
			headerLine = headerLine.NormalizeFormatting(true);
			var blocks = headerLine.Split(' ');
			if (blocks.Length < 2) // not valid formatting
			{
				fail();
				return;
			}
			Name = blocks[0]; // split by spaces, take the first word
			if (blocks[1].ToLower() != "macro") // not a macro
			{
				fail();
				return;
			}
			if (blocks.Length > 2) // more text after macro keyword suggests parameters
			{
				var paramStart = headerLine.IndexOf("macro ", StringComparison.OrdinalIgnoreCase); // find where parameters start
				var parameterText = headerLine.Substring(paramStart + 6); // take all text past macro
				Parameters = parameterText.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); // split by commas
			}

			var lines = new List<ASMChunk>();
			Encoding charSet = FileStream.CurrentEncoding;
			while (!FileStream.EndOfStream) {
				long currentPosition = FileStream.GetActualPosition(); // store position
				var line = FileStream.ReadLine().RemoveEscapes(); // PEEK at the line
				runningLength += FileStream.GetActualPosition() - currentPosition;
				FileStream.BaseStream.Seek(currentPosition, SeekOrigin.Begin); // move it back to where it was to complete the PEEK
				FileStream.DiscardBufferedData();
				var module = ASMImporter.ProcChunk(OriginalFileName, context, FileStream, ref charSet); // check each line to see what it is
				lines.Add(module);
				if (line.Trim().Equals("endm", StringComparison.OrdinalIgnoreCase)) // end macro spotted
					break;
			}
			Lines = lines.ToArray();
			Length = runningLength;

			// Look for positional parameters in lines
			if (Parameters.Length < 1) {
				var lstPositionals = new List<string>();
				foreach (var al in lines.OfType<ASMLine>()) {
					var colMatches = Regex.Matches(al.Text, @"\\([0-9]+)");
					// ReSharper disable once RedundantEnumerableCastCall (wrong advice)
					lstPositionals.AddRange(colMatches.OfType<Match>().Select(m => m.Value));
				}
				lstPositionals.Sort(); // does a textual sort, which will be wrong for over 9 positional parameters
				Parameters = lstPositionals.Distinct().ToArray();
			}
		}

		public override string ToString()
		{
			return base.ToString() + $": {Name}({Parameters.Length} args) {{ {Lines.Length} lines }}";
		}

		public override string ToString(string format, IFormatProvider formatProvider)
		{
			if (String.IsNullOrEmpty(format)) {
				format = "g";
			}
			if (formatProvider == null) {
				formatProvider = CultureInfo.CurrentCulture;
			}

			var stbOutput = new StringBuilder();
			if (Char.IsUpper(format[0])) {
				this.AppendHeader(stbOutput, formatProvider, format);
				stbOutput.Append("Line: ").Append(formatProvider, format, this.Line + 1).Append(", Length: ");
				stbOutput.Append(formatProvider, format, this.Lines.Length).Append(" lines");
				if (Parameters.Length > 0) {
					stbOutput.AppendLine().Append(formatProvider, format, Parameters.Length);
					stbOutput.Append(" Parameters: ").Append(String.Join(", ", this.Parameters));
				}
			} else {
				stbOutput.Append(base.ToString(format, formatProvider));
				stbOutput.Append(": ").Append(this.Name).Append(formatProvider, format, Parameters.Length);
				stbOutput.Append(" args) {").Append(formatProvider, format, Lines.Length).Append(" lines }");
			}
			return stbOutput.ToString();
		}
	}
}
