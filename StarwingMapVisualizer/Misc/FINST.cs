using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;

namespace StarwingMapVisualizer.Misc
{
	/// <summary>
	/// Represents an instance of a file opened in an editor
	/// </summary>
	/// <typeparam name="TFile">The type of file this is interpreted as</typeparam>
	/// <typeparam name="TState">The type of editor it is used in</typeparam>
	/// <typeparam name="TTag">The object used to find this instance, like a tab at the top of the screen</typeparam>
	public class FINST<TFile, TState, TTag> where TFile : IImporterObject
	{
		internal FileInfo OpenFile;
		internal TFile FileImportData;

		internal TTag Tab;
		internal TState StateObject;
	}

	/// <summary>
	/// A class that represents an instance of a file opened in the ASMControl
	/// <para>One per tab at the top of the editor</para>
	/// </summary>
	public class ASM_FINST<TEditor> : FINST<ASMFile, TEditor, TabItem>
	{
		internal Dictionary<ASMChunk, Run> SymbolMap;
		internal Dictionary<long, Inline> NewLineMap { get; } = new Dictionary<long, Inline>();
		internal TEditor EditorScreen => StateObject;
	}

	public class MAP_FINST : FINST<MAPFile, MAP_FINST.MAPEditorState, TabItem>
	{
		public class MAPEditorState
		{
			public bool Loaded => ContentControl != null;
			public Panel ContentControl { get; set; }

			public double LevelWidth = 0;

			public List<MAPScript> Subsections { get; } = new List<MAPScript>();
		}

		public MAP_FINST()
		{
			StateObject = new MAPEditorState();
		}
	}
}
