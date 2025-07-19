using System;
using System.Collections.Generic;
using System.Linq;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MISC
{
    public static class SymbolOperations
    {
        public static ASMMacro MatchMacro(IEnumerable<ASMFile> Imports, string SymbolName)
        {
            Imports = Imports.DistinctBy(x => x.OriginalFilePath);
            return MatchMacro(Imports.SelectMany(x => x.Chunks.OfType<ASMMacro>()), SymbolName);
        }

        private static ASMMacro MatchMacro(IEnumerable<ASMMacro> SymbolNameList, string SymbolName)
        {
            var macroNames = SymbolNameList.Select(x => x.Name.ToLower());
            var block = SymbolName;
            var macros = SymbolNameList;
            if (macroNames.Contains(block.ToLower())) // macro found
            {
                var macroName = block;
                return macros.FirstOrDefault(x => string.Equals(x.Name, macroName, StringComparison.InvariantCultureIgnoreCase));
            }
            return null;
        }
    }
}
