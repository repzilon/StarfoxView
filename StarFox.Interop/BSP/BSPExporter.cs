using g4;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.COLTAB.DEF;
using System.Drawing;
using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.BSP
{
    /// <summary>
    /// Exports a SHAPE to an FBX file
    /// <para/> Currently just exports geometry
    /// </summary>
    public class BSPExporter
    {
        public const string FILE_EXTENSION = ".obj"; 

        public struct BSPIOWriteResult
        {
            public string Descriptor { get; }
            public string Message { get; }
            public bool Successful { get; }
            

            public BSPIOWriteResult(IOWriteResult Other, string? appendMsg = null)
            {
                Descriptor = nameof(Other);
                Successful = Other.code == IOCode.Ok;
                Message = appendMsg + Other.message;
            }
            internal BSPIOWriteResult(string descriptor, string message, bool successful)
            {
                Descriptor = descriptor;
                Message = message;  
                Successful = successful;
            }

            public static BSPIOWriteResult Cancelled = new BSPIOWriteResult("Cancelled", "Operation was cancelled.", false);
            public static BSPIOWriteResult Faulted(Exception exception) => new BSPIOWriteResult(exception.GetType().Name, $"An error has occurred: {exception.Message}", false);
        }

        /// <summary>
        /// Options to use when exporting shapes using the <see cref="BSPExporter"/>
        /// </summary>
        public class BSPExportOptions
        {
            /// <summary>
            /// Process color information on this shape and apply as Vertex Colors?
            /// </summary>
            public bool ColorActivated { get; set; }
            /// <summary>
            /// Process color animations?
            /// </summary>
            public bool ColorAnimationsActivated { get; set; }
            /// <summary>
            /// Process elements of this shape that only are comprised of two 3D positions?
            /// </summary>
            public bool ProcessLines { get; set; }

            public static BSPExportOptions Default = new BSPExportOptions()
            {
                ColorActivated = true,
                ColorAnimationsActivated = true,
                ProcessLines = true
            };
        }

        /// <summary>
        /// Exports a <see cref="BSPShape"/> to the given file name -- needs to be a <see cref="FILE_EXTENSION"/> file.
        /// <para/>This function applies Colors using the supplied parameters.
        /// </summary>
        /// <param name="FileName">The destination file name</param>
        /// <param name="Shape">The shape to export</param>
        /// <param name="Group">Coloring information for this <see cref="BSPShape"/></param>
        /// <param name="Palette">The colors to paint this <see cref="BSPShape"/> with</param>
        /// <param name="Frame">The frame of animation to export</param>
        /// <param name="ColorTable">Used for ColorAnimations -- should be the Project Color Table</param>
        /// <param name="Palt">The Color Animation table to apply, usually is BLUE.COL</param>
        /// <returns></returns>
        public static BSPIOWriteResult ExportShapeFBX(string FileName, BSPShape Shape, COLGroup Group, 
            SFPalette Palette, int Frame, COLTABFile ColorTable, COL Palt, BSPExportOptions Options = default)
        {
            if (Options == default)
                Options = BSPExportOptions.Default;
            if (!FileName.EndsWith(FILE_EXTENSION)) return new BSPIOWriteResult(nameof(NotSupportedException), 
                "Only accepting *.OBJ file extensions at this time.", false);

            string userMsg = "";

            List<Vector3d> vertices = new();
            List<int> indices = new List<int>();
            List<Vector3f> normals = null;

            int overallIndex = 0;

            //Set up mesh geometry
            foreach (var face in Shape.Faces)
            {
                if (face.PointIndices.Length % 3 != 0) // must be triangulated and not quads or lines
                {
                    if (face.PointIndices.Length == 2 && Options.ProcessLines)
                        PushLine(vertices, indices, Shape, face, Frame);
                    continue;
                }
                
                //order the indices in order that they appear in the code (as if they're not already?)
                var orderedIndicies = face.PointIndices.OrderBy(x => x.Position).ToArray();
                for (int i = 0; i < face.PointIndices.Count(); i++)
                { 
                    var pointRefd = orderedIndicies[i]; // get the PointReference
                    var point = Shape.GetPointOrDefault(pointRefd.PointIndex, Frame); // find the referenced point itself
                    if (point == null)
                        return new BSPIOWriteResult(nameof(InvalidDataException), $"Point {pointRefd} is referenced yet not present on {Shape.Header.Name}", false); // uh, we didn't find it.
                    vertices.Add(new Vector3d(point.X, point.Y, point.Z)); // sweet found it, push it to our Vertex Buffer
                    indices.Add(overallIndex); // add the index
                    overallIndex++;
                }                              
            }
            //build the mesh
            DMesh3 meshData = DMesh3Builder.Build(vertices, indices, normals);
            try
            { // check if it is valid.. our models might not be compatible with this algorithm?
                meshData.CheckValidity();
            }
            catch (Exception e) 
            {
                userMsg += e.Message + "... this model might not look right. "; // alert the user there may be an issue
            }
            meshData.EnableVertexColors(new Vector3f(0, 1, .25));
            overallIndex = 0;
            //Setup colors
            if (Options.ColorActivated)
                foreach (var face in Shape.Faces)
                {
                    int count = face.PointIndices.Count();
                    if (face.PointIndices.Count() == 2)
                        count = 4;
                    for (int i = 0; i < count; i++)
                    {
                        var colorRef = Group.Definitions.ElementAt(face.Color);
                        meshData.SetVertexColor(overallIndex++, GetMaterialEntry(colorRef, Palette, ColorTable, Palt, Frame, Options.ColorAnimationsActivated));
                    }
                }
            return new BSPIOWriteResult(StandardMeshWriter.WriteMesh(FileName, meshData, new WriteOptions()
            {
                bWriteBinary = false,
                bPerVertexNormals = false,
                bPerVertexColors = true,
                bWriteGroups = false,
                bPerVertexUVs = false,
                bCombineMeshes = false,
                bWriteMaterials = false,
                ProgressFunc = null,
                RealPrecisionDigits = 15
            }),userMsg);
        }

        public static bool PushLine(List<Vector3d> Verticies, List<int> Indices, Vector3d Point1, Vector3d Point2)
        {
            double thickness = .5;
            int index = Verticies.Count(); // used to push indices
            Verticies.Add(new(Point1.x, Point1.y, Point1.z)); // i
            Verticies.Add(new(Point1.x - thickness, Point1.y, Point1.z + thickness)); // i + 1            
            Verticies.Add(new(Point2.x, Point2.y, Point2.z)); // i + 2
            Verticies.Add(new(Point2.x + thickness, Point2.y, Point2.z - thickness)); // i + 3
            Indices.Add(index);
            Indices.Add(index + 1);
            Indices.Add(index + 2);
            Indices.Add(index);
            Indices.Add(index + 3);
            Indices.Add(index + 2);
            //RECAP: We made a rectangle that looks like a line. It has some depth to be viewable at oblique angles
            return true;
        }
        /// <summary>
        /// Puts a point into the specified Geometry
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="Shape"></param>
        /// <param name="Face"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public static bool PushLine(List<Vector3d> Verticies, List<int> Indices, BSPShape Shape, in BSPFace Face, int Frame)
        { // Pushes a line to the mesh geometry provided to this function
            var ModelPoints = Face.PointIndices.Select(x => Shape.GetPointOrDefault(x.PointIndex, Frame)).Where(y => y != default).ToArray();
            if (ModelPoints.Length != 2) return false; // not a line!!                        
            return PushLine(Verticies, Indices, new Vector3d(ModelPoints[0].X, ModelPoints[0].Y, ModelPoints[0].Z),
                new Vector3d(ModelPoints[1].X, ModelPoints[1].Y, ModelPoints[1].Z));
        }

        /// <summary>
        /// Tries to create a new palette using the COLTABFile added to the project and a ColorPalettePtr
        /// </summary>
        /// <param name="ColGroupName">The name of the COLGroup item to locate</param>
        /// <param name="PaletteMap">The list of all loaded palettes to choose from</param>
        /// <param name="Palette">The created SFPalette</param>
        /// <param name="Group">The created COLGroup object</param>
        /// <param name="ColorPaletteName">Optionally, you can specify a COLTable to use instead of BLUE.COL</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static bool CreateSFPalette(string ColGroupName, COLTABFile ColorTable, COL Palt, out SFPalette Palette, out COLGroup Group)
        {
            COL? palette = Palt;
            COLGroup? group = default;
            ColorTable?.TryGetGroup(ColGroupName, out group);           
            if (palette == null || group == null)
                throw new Exception($"There was a problem loading the palette and/or color table group: {ColGroupName}.");
            Group = group;
            SFPalette sfPalette = new SFPalette("ColAnimRendr", in palette, in group);
            sfPalette.GetPalette();
            Palette = sfPalette;
            return true;
        }

        static Vector3f GetColor(COLDefinition.CallTypes Type, int colIndex, SFPalette palette)
        { // Get a color for a COLDefinition from the sfPalette
            var fooColor = System.Drawing.Color.Blue;
            switch (Type)
            {
                case COLDefinition.CallTypes.Collite: // diffuse                    
                    fooColor = palette.Collites[colIndex];
                    break;
                case COLDefinition.CallTypes.Colnorm: // normal? not sure.
                    fooColor = palette.Colnorms[colIndex];
                    break;
                case COLDefinition.CallTypes.Colsmooth: // not implemented
                case COLDefinition.CallTypes.Coldepth: // No reaction to angle
                    fooColor = palette.Coldepths.ElementAtOrDefault(colIndex).Value;
                    break;
            }
            return new(fooColor.R/255.0, fooColor.G/255.0, fooColor.B/255.0);
        }
        static Vector3f GetMaterialEntry(COLDefinition definition, SFPalette currentSFPalette, COLTABFile ColorTable, COL Palt, int MaterialAnimationFrame = 0, bool ColorAnimationsEnabled = true)
        {
            int colIndex = 0;
            switch (definition.CallType)
            { // depending on call type we handle this material differently
                case COLDefinition.CallTypes.Collite: // diffuse
                case COLDefinition.CallTypes.Coldepth: // emissive, kinda
                case COLDefinition.CallTypes.Colnorm: // normal? not sure.
                case COLDefinition.CallTypes.Colsmooth: // not implemented
                    { // default, push the color to the model
                        if (definition is COLTexture)
                            goto case COLDefinition.CallTypes.Texture;
                        colIndex = ((ICOLColorIndexDefinition)definition).ColorByte;
                        return GetColor(definition.CallType, colIndex, currentSFPalette);
                    }
                case COLDefinition.CallTypes.Animation: // color animation
                    {
                        if (!ColorAnimationsEnabled) break;

                        var animDef = definition as COLAnimationReference; // find anim definition
                                                                           //attempt to make a palette for this animation
                        if (!CreateSFPalette(animDef.TableName, ColorTable, Palt, out var animSFPal, out var animGroup)) break;
                        int index = MaterialAnimationFrame > -1 ? MaterialAnimationFrame %
                            animGroup.Definitions.Count : 0; // adjust color based on MatAnimFrame parameter
                        var animMemberDefinition = animGroup.Definitions.ElementAt(index); // jump to color
                        return GetColor(animMemberDefinition.CallType, // finally get the color from the animPalette
                            ((ICOLColorIndexDefinition)animMemberDefinition).ColorByte,
                            animSFPal);
                    }
                    break;
                case COLDefinition.CallTypes.Texture:
                    
                    break;
            }
            return new Vector3f(1,1,1);
        }
    }
}
