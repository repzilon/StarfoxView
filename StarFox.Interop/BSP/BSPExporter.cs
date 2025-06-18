using g4;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.COLTAB.DEF;

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

        public static BSPIOWriteResult ExportShapeFBX(string FileName, BSPShape Shape, COLGroup Group, SFPalette Palette, int Frame, int MaterialAnimationFrame)
        {
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
                    continue;
                
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
            foreach (var face in Shape.Faces)
            {
                if (face.PointIndices.Length % 3 != 0) // must be triangulated and not quads or lines
                    continue;

                for (int i = 0; i < face.PointIndices.Count(); i++)
                {
                    //int vID = face.PointIndices[i].PointIndex;
                    var colorRef = Group.Definitions.ElementAt(face.Color);
                    meshData.SetVertexColor(overallIndex++, GetMaterialEntry(colorRef, Palette));
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
        static Vector3f GetMaterialEntry(COLDefinition definition, SFPalette currentSFPalette)
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
                case COLDefinition.CallTypes.Texture:
                    
                    break;
            }
            return new Vector3f(1,1,1);
        }
    }
}
