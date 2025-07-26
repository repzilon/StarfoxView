using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
	/// <summary>
	/// A PathObj Macro Invocation level Event
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
			Callsite = Line;
			var structure = Line.StructureAsMacroInvokeStructure;
			if (structure == null) return;
			EventName = structure.MacroReference.Name;
			Delay = TryParseOrDefault(structure.TryGetParameter(0).Value); // parameter 0 is frame
			X = TryParseOrDefault(structure.TryGetParameter(1).Value); // parameter 1 is x
			Y = TryParseOrDefault(structure.TryGetParameter(2).Value); // parameter 2 is y
			Z = TryParseOrDefault(structure.TryGetParameter(3).Value); // parameter 3 is z
			ShapeName = structure.TryGetParameter(4).Value ?? ""; // parameter 4 is shape
			PathName = structure.TryGetParameter(5).Value ?? ""; // parameter 5 is path
			HP = TryParseOrDefault(structure.TryGetParameter(6).Value ?? ""); // parameter 6 is hp
			AP = TryParseOrDefault(structure.TryGetParameter(7).Value ?? ""); // parameter 7 is ap
		}
	}
}
