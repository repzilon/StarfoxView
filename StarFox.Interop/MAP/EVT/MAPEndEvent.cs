using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// Represents a mapwait event.
    /// <para><code>mapwait time</code></para>
    /// </summary>
    public class MAPEndEvent : MAPEvent
    {
        protected override string[] CompatibleMacros { get; } =
        {
            "mapend"
        };

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            //COMPATIBILITY WITH STARFOX**
            CtrlOptCode = MAPCtrlVars.ctrlend;        
        }
    }
}
