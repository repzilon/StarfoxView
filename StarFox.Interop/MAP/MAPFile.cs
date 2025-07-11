﻿using System.Collections.Generic;
using StarFox.Interop.ASM;

namespace StarFox.Interop.MAP
{
	/// <summary>
	/// Represents a Map Script File
	/// </summary>
	public class MAPFile : ASMFile
    {
        /// <summary>
        /// Scripts in this file, accessible by their label name. See: <see cref="MAPScriptHeader.LevelMacroName"/>
        /// <para/>
        /// One <see cref="MAPFile"/> can have many level scripts inside
        /// <para/> They should start with a label indicating where it starts
        /// and end with a <c>mapend</c> event
        /// </summary>
        public Dictionary<string, MAPScript> Scripts { get; } = new Dictionary<string, MAPScript>();
        /// <summary>
        /// Creates a new MAPFile representing the referenced file
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal MAPFile(string OriginalFilePath) : base(OriginalFilePath) {

        }
        internal MAPFile(ASMFile From) : base(From)
        {

        }
    }
}
