using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// Represents a mapwait event.
    /// <para><code>mapwait time</code></para>
    /// </summary>
    public class MAPWaitEvent : MAPEvent, IMAPDelayEvent
    {
        public int Delay { get; set; }

        protected override string[] CompatibleMacros { get; } =
        {
            "mapwait"
        };

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Delay = TryParseOrDefault(structure.TryGetParameter(0)?.ParameterContent);
            //COMPATIBILITY WITH STARFOX**
            if (Delay != 0)
            {
                if ((Delay >> 4)-256 < 0)
                {
                    CtrlOptCode = MAPCtrlVars.ctrlmapwait2;
                    Delay >>= 4;
                }
                else                
                    CtrlOptCode = MAPCtrlVars.ctrlmapwait;                                   
            }
        }
    }
}
