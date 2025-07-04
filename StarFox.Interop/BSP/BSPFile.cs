﻿using System.Collections.Generic;
using System.Text;
using StarFox.Interop.ASM;
using StarFox.Interop.BSP.SHAPE;

namespace StarFox.Interop.BSP
{
    public class BSPFile : ASMFile
    {
        /// <summary>
        /// The shapes added in this BSP file
        /// <para>Usually shapes come in large files with many other shapes defined alongside them.</para>
        /// </summary>
        public HashSet<BSPShape> Shapes { get; internal set; } = new HashSet<BSPShape>();
        /// <summary>
        /// These are shapes that are just a standalone definition. Developers used a trick to get multiple shapes out of one set of faces and
        /// points through clever use of inline labels that reference the same shape code.
        /// </summary>
        public HashSet<BSPShape> BlankShapes { get; internal set; } = new HashSet<BSPShape>();
        /// <summary>
        /// Errors that happened while exporting are dumped here
        /// </summary>
        public StringBuilder ImportErrors { get; } = new StringBuilder();

        public HashSet<string> ShapeHeaderEntries { get; } = new HashSet<string>();
        /// <summary>
        /// Finds items that are in <see cref="ShapeHeaderEntries"/> yet not in <see cref="Shapes"/>
        /// </summary>
        /// <returns></returns>
        /*
        public IEnumerable<string> GetShapeHeaderDiscrepancies()
        {
            List<string> discrepancies = new();
            foreach(var headerItem in ShapeHeaderEntries)
            {
                if (!Shapes.ContainsKey(headerItem))
                    discrepancies.Add(headerItem);
            }
            return discrepancies;
        }*/

        internal BSPFile(string OriginalFilePath) : base(OriginalFilePath)
        {

        }
        internal BSPFile(ASMFile From) : base(From)
        {

        }
    }
}
