﻿using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// A PathObj Macro Invokation level Event
    /// <para><code>[pathobj][pathspecial][pathcspecial] frame,x,y,z,shape,path,hp,ap</code></para>
    /// </summary>
    public class MAPPathObjectEvent : MAPEvent, IMAPDelayEvent, IMAPLocationEvent, IMAPShapeEvent, IMAPPathEvent, IMAPHealthAttackEvent
    {
        public int Delay { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int ShapeDefinitionLabel { get; set; }
        public string ShapeName { get; set; }
        public string PathName { get; set; }
        public int HP { get; set; }
        public int AP { get; set; }

        protected override string[] CompatibleMacros { get; } =
        {
            "pathobj", "pathspecial", "pathcspecial"
        };

        public MAPPathObjectEvent() : base()
        {

        }
        public MAPPathObjectEvent(ASMLine Line) : base(Line)
        {

        }

        protected override void Parse(ASMLine Line)
        {
            int tryParseOrDefault(string content)
            {
                if (string.IsNullOrEmpty(content)) return 0;
                if (int.TryParse(content, out int result)) { return result; }
                return 0;
            }
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Delay = tryParseOrDefault(structure.TryGetParameter(0)?.ParameterContent); // parameter 0 is frame
            X = tryParseOrDefault(structure.TryGetParameter(1)?.ParameterContent); // parameter 1 is x
            Y = tryParseOrDefault(structure.TryGetParameter(2)?.ParameterContent); // parameter 2 is y
            Z = tryParseOrDefault(structure.TryGetParameter(3)?.ParameterContent); // parameter 3 is z
            ShapeName = structure.TryGetParameter(4)?.ParameterContent ?? ""; // parameter 4 is shape
            PathName = structure.TryGetParameter(5)?.ParameterContent ?? ""; // parameter 5 is path
            HP = tryParseOrDefault(structure.TryGetParameter(6)?.ParameterContent ?? ""); // parameter 6 is hp
            AP = tryParseOrDefault(structure.TryGetParameter(7)?.ParameterContent ?? ""); // parameter 7 is ap
        }
    }
}
