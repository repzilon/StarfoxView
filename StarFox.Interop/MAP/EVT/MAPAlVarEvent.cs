﻿using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// Represents a map variable event
    /// <para><code>mapalvar name,value</code></para>
    /// </summary>
    public class MAPAlVarEvent : MAPEvent, IMAPNamedEvent, IMAPValueEvent
    {
        /// <summary>
        /// The name of this variable
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The value of the variable
        /// </summary>
        public string Value { get; set; }

        protected override string[] CompatibleMacros { get; } =
        {
            "setalvar"
        };

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Name = structure.TryGetParameter(0)?.ParameterContent ?? ""; // parameter 0 is name
            Value = structure.TryGetParameter(1)?.ParameterContent ?? ""; // parameter 0 is name
        }
    }
}
