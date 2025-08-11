namespace StarFox.Interop.ASM.TYP.STRUCT
{
	public enum ASMLineType
	{
		Unknown,
		Label,
		Define,
		Macro,
		MacroInvoke,
		MacroInvokeParameter
	}

	public interface IASMLineStructure
    {
	    string Symbol { get; }
    }
}
