using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM
{
	public abstract class ImporterContext<T> where T : IImporterObject
	{
		public T[] Includes { get; set; }
		public T CurrentFile { get; set; }
		public string CurrentFilePath { get; set; }
		public int CurrentLine { get; set; }
	}

	public class ASMImporterContext : ImporterContext<ASMFile>
	{
	}

	/// <summary>
	/// A custom-written basic ASM code object importer
	/// </summary>
	public class ASMImporter : CodeImporter<ASMFile>
	{
		internal ASMImporterContext Context { get; }

		internal ASMFile[] CurrentIncludes => Context?.Includes;

		public ASMImporter()
		{
			Context = new ASMImporterContext()
			{
				CurrentFilePath = null,
			};
		}

		public ASMImporter(string FilePath, params ASMFile[] Imports) : this()
		{
			Context.CurrentFilePath = FilePath;
			SetImports(Imports);
			_ = ImportAsync(FilePath);
		}

		/// <summary>
		/// You can import other ASM files that have symbol definitions in them to have those symbols linked.
		/// </summary>
		/// <param name="Imports"></param>
		public override void SetImports(params ASMFile[] Imports)
		{
			Context.Includes = Imports;
		}

		public override async Task<ASMFile> ImportAsync(string FilePath)
		{
			Context.CurrentFilePath = FilePath;
			var newFile = Context.CurrentFile = new ASMFile(FilePath);
			Context.CurrentLine = 0;
#if NETFRAMEWORK || NETSTANDARD
			var charSet = Encoding.Default;
#else
			var charSet = Encoding.Latin1;
#endif
			// ReSharper disable once UseAwaitUsing (needs C# 8, but we are locked to 7.3)
			using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read)) {
				StreamReader reader = null;
				try {
					reader = ImportCore(FilePath, fs, ref charSet, newFile);
				} catch (DecoderFallbackException) {
					// Bad encoding detected, reset reading
					newFile.Chunks.Clear();
					reader?.Dispose();
					fs.Position = 0;
					Context.CurrentLine = 0;
					// Re-read with the detected encoding
					reader = ImportCore(FilePath, fs, ref charSet, newFile);
				} finally {
					reader?.Dispose();
				}
			}
			return ImportedObject = newFile;
		}

		private StreamReader ImportCore(string filePath, FileStream stream, ref Encoding charSet, ASMFile newFile)
		{
			var reader = new StreamReader(stream, charSet, true);
			charSet = reader.CurrentEncoding;
			while (!reader.EndOfStream) {
				var chunk = ProcChunk(filePath, Context, reader­, ref charSet); // process this line as a new chunk
				if (chunk != null) {
					newFile.Chunks.Add(chunk);
				}
			}
			return reader;
		}

		private static void GuessEncoding(string readLine, string indicator, ref Encoding textEncoding)
		{
			if (readLine.Contains(indicator)) {
				try {
					var encodingName = readLine.Replace(indicator, "").Replace(".", "").Trim();
					textEncoding = Encoding.GetEncoding(encodingName);
				} catch (ArgumentException) {
					// ignore fake encoding name
				}
			}
		}

		/// <summary>
		/// Process the current line of the FileStream as a chunk header
		/// </summary>
		/// <param name="context"></param>
		/// <param name="fs"></param>
		/// <param name="filePath"></param>
		/// <param name="textEncoding"></param>
		/// <returns></returns>
		internal static ASMChunk ProcChunk(string filePath, ASMImporterContext context, StreamReader fs, ref Encoding textEncoding)
		{
			// IMPORTANT : returns the byte position, which is needed to seek inside the base FileStream
			// This is different from character position, and both will drift away with a variable-length
			// character coding such as UTF-8.
			long position = fs.GetActualPosition(); 

			var rawLine = fs.ReadLine();
			var header = rawLine.RemoveEscapes(); // read line
			if (header.Contains('\uFFFD')) {
				throw new DecoderFallbackException("Detected character encoding error in " +
				                                   Path.GetFileName(filePath) + ".");
			} else if (!textEncoding.Equals(fs.CurrentEncoding)) {
				throw new DecoderFallbackException("Character set declared in " + Path.GetFileName(filePath) + " is " +
				                                   textEncoding.WebName + " and does not match " +
				                                   fs.CurrentEncoding.WebName + " of the file reader.");
			} else {
				GuessEncoding(header, "; Character set of this file is ", ref textEncoding);
				GuessEncoding(header, "; This file is encoded in ", ref textEncoding);
			}

			context.CurrentLine++; // increment line register
			var type = ASMChunk.Conjecture(header); // investigate what it is

			ASMChunk chunk;
			var lineNo = context.CurrentLine;
			if (type == ASMChunks.Comment) {
				chunk = new ASMComment(filePath, position, lineNo);
				chunk.Parse(fs);
			} else if (type == ASMChunks.Macro) {
				chunk = new ASMMacro(position, context, lineNo);
				chunk.Parse(fs);
			} else {
				chunk = new ASMLine(position, context, lineNo)
				{
					IsUnknownType = type == ASMChunks.Unknown,
				};
				chunk.Parse(fs);
			}

			if (chunk is ASMLine line) // check if this is a LINE of Assembly code
			{
				if (string.IsNullOrWhiteSpace(line.Text))// is the string empty or just spaces?
					return null; // return nothing if this isn't useful.
			}
			return chunk;
		}

		public override ImporterContext<TInclude> GetCurrentContext<TInclude>()
		{
			return Context as ImporterContext<TInclude>;
		}
	}
}
