﻿using System;
using System.IO;
using StarFox.Interop.ASM.TYP.STRUCT;

namespace StarFox.Interop.ASM.TYP
{
	/// <summary>
	/// The definition of a constant, with reference to the <see cref="ASMLine"/> that declares the constant.
	/// <para>To find the callsite and/or line information, reference the <see cref="ASMLine.StructureAsDefineStructure"/></para>
	/// </summary>
	public class ASMConstant : ASMChunk, IASMNamedSymbol
	{
		/// <summary>
		/// Creates an ASMConstant from the line that declares it.
		/// <para>This line needs to have <see cref="ASMDefineLineStructure"/> applied!</para>
		/// </summary>
		/// <param name="definitionLine"></param>
		public ASMConstant(ASMLine definitionLine)
		{
			DefinitionLine = definitionLine;
			OriginalFileName = definitionLine.OriginalFileName;
			Position = definitionLine.Position;
			Length = definitionLine.Length;
		}
		/// <summary>
		/// <see cref="DefinitionLine"/> is the source-of-truth. This property's set accessor is ignored.
		/// </summary>
		public override int Line { get => DefinitionLine.Line; internal set => _ = value; }
		public override ASMChunks ChunkType => ASMChunks.Constant;
		/// <summary>
		/// The name of the constant
		/// </summary>
		public string Name => DefinitionLine.StructureAsDefineStructure.Symbol;
		/// <summary>
		/// The value of the constant
		/// </summary>
		public string Value => DefinitionLine.StructureAsDefineStructure.Value;
		/// <summary>
		/// The line information where this constant is defined.
		/// </summary>
		public ASMLine DefinitionLine { get; }

		public override void Parse(StreamReader FileStream)
		{
			throw new InvalidOperationException("ASMConstant is parsed through ASMLine parse procedure.");
		}
	}
}
