using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.BSP;
using StarFox.Interop.MISC;

namespace StarFox.Interop.MAP.CONTEXT
{
    /// <summary>
    /// Contains information on what level context definitions are defined in the given file.
    /// <para>This should generally be used with the file: <code>BGS.ASM</code></para>
    /// </summary>
    public class MAPContextFile : ASMFile
    {
        /// <summary>
        /// Maps the <see cref="MAPContextDefinition"/> to it's name as it appears in the code
        /// </summary>
        public Dictionary<string, MAPContextDefinition> Definitions { get; set; } = new Dictionary<string, MAPContextDefinition>();
        public StringBuilder LoadErrors { get; } = new StringBuilder();
        /// <summary>
        /// Creates a blank <see cref="MAPContextFile"/> with the given original file path.
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        public MAPContextFile(string OriginalFilePath) : base(OriginalFilePath)
        {

        }
        /// <summary>
        /// Creates a new <see cref="MAPContextFile"/> with information sourced from the given <see cref="ASMFile"/>
        /// </summary>
        /// <param name="From"></param>
        public MAPContextFile(ASMFile From) : base(From)
        {

        }
    }

    /// <summary>
    /// This will import a file containing context about the levels defined in Starfox.
    /// <para>This should generally be used with the file: <code>BGS.ASM</code></para>
    /// </summary>
    internal class MAPContextImporter : CodeImporter<MAPContextFile>
    {
        private MAPContextImporterContext _context;
        private ASMImporter _baseImporter = new ASMImporter();
        /// <summary>
        /// Creates a new <see cref="MAPContextImporter"/>
        /// </summary>
        public MAPContextImporter()
        {

        }
        public override void SetImports(params ASMFile[] Includes)
        {
            _baseImporter.SetImports(Includes);
        }
        private void ReadLine(ASMLine currentLine)
        {
            _context.CheckStartDefinition(currentLine); // CHECK IF WE ARE STARTING A NEW DEF
            _context.CheckLineContents(currentLine);
        }
        public override async Task<MAPContextFile> ImportAsync(string FilePath)
        {
            var baseImport = await _baseImporter.ImportAsync(FilePath);
            if (baseImport == default) throw new InvalidOperationException("That file could not be parsed.");
            return ImportAsync(baseImport);
        }
        public MAPContextFile ImportAsync(ASMFile BGSASMFile)
        {
            var file = ImportedObject = new MAPContextFile(BGSASMFile); // from ASM file
            _context = new MAPContextImporterContext(file)
            {
                Includes = _baseImporter.CurrentIncludes
            };
            ASMExtensions.BeginConstantsContext(_context.Includes);
            foreach (var line in file.Chunks.OfType<ASMLine>())
            {
                if (!line.HasStructureApplied)
                {
                    var chunks = line.Text.NormalizeFormatting().Split(' ');
                    if (chunks.Length == 1 && chunks[0].Length > 0 && !string.IsNullOrWhiteSpace(chunks[0]))
                    {
                        // only one word, isn't blank and starts with bg_
                        if (chunks[0].StartsWith("bg_"))
                            _context.StartDefinition(chunks[0]);
                        else if (chunks[0] == "initmode1")
                            break;
                    }
                    continue;
                }
                if (!(line.Structure is ASMMacroInvokeLineStructure)) continue; // we can't do much with these right now
                ReadLine(line); // read the current line to find information
            }
            ASMExtensions.EndConstantsContext();
            return file;
        }
        public override ImporterContext<IncludeType> GetCurrentContext<IncludeType>() => _context as ImporterContext<IncludeType>;
    }
}
