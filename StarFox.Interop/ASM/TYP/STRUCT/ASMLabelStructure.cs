using System;
using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP.STRUCT
{
    /// <summary>
    /// Represents an ASMLine that defines a constant
    /// </summary>
    public class ASMLabelStructure : IASMLineStructure
    {
        public ASMLabelStructure(string name)
        {
            Symbol = name;
        }
        /// <summary>
        /// The name given to this Constant
        /// </summary>
        public string Symbol { get; private set; }

		/// <summary>
		/// Tries to parse this line as a macro invocation
		/// </summary>
		/// <param name="input"></param>
		/// <param name="result"></param>
		/// <exception cref="NotImplementedException"></exception>
		public static bool TryParse(string input, out ASMLabelStructure result)
        {
            var originalText = input;
            input = input.NormalizeFormatting();
            result = default;
            if (!input.Contains(':')) return false;
            var name = input.Substring(0, input.IndexOf(':'));
            result = new ASMLabelStructure(name);
            return true;
        }
    }
}
