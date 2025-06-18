using CSMath;
using MeshIO;
using MeshIO.Entities.Geometries;
using MeshIO.FBX;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshIO.Shaders;
using StarFox.Interop.GFX.COLTAB.DEF;

namespace StarFox.Interop.BSP
{
    /// <summary>
    /// Exports a SHAPE to an FBX file
    /// <para/> Currently just exports geometry
    /// </summary>
    public class BSPExporter
    {
        public static void ExportShapeFBX(string FileName, BSPShape Shape, COLGroup Group, SFPalette Palette, int Frame, int MaterialAnimationFrame)
        {
            Scene scene = new Scene();
            Node modelNode = new Node(Shape.Header.Name);
            scene.RootNode.Nodes.Add(modelNode);
            Dictionary<int, Mesh> DrawGroups = new();
            Mesh GetDrawGroup(int ColorIndex)
            {
                if (DrawGroups.TryGetValue(ColorIndex, out var geom))
                    return geom;
#if false
                Node newDrawGroup = new Node($"MATERIAL{ColorIndex}");
                geom = new($"MATERIAL{ColorIndex}_Mesh");
                newDrawGroup.Entities.Add(geom);
                modelNode.Nodes.Add(newDrawGroup);
#endif
                modelNode.Entities.Add(geom = new Mesh($"MATERIAL{ColorIndex}_Mesh"));

                var definition = Group.Definitions.ElementAt(ColorIndex);
                var color = GetMaterialEntry(definition, Palette);
                //newDrawGroup.Materials.Add(new Material($"{definition.CallType}{ColorIndex}"));

                DrawGroups.Add(ColorIndex, geom);
                return geom;
            }

            foreach(var face in Shape.Faces)
            {                
                if (face.PointIndices.Length % 3 == 0 || face.PointIndices.Length % 4 == 0)
                {
                    var geom = GetDrawGroup(0);
                    XYZ getPointRef(int PointRef)
                    {
                        var point = Shape.GetPoint(PointRef, Frame);
                        return new(point.X, point.Y, point.Z);
                    }
                    geom.AddPolygons(face.PointIndices.Select(x => getPointRef(x.PointIndex)).ToArray());
                }
            }

            FbxWriter writer = new FbxWriter(FileName, scene);
            writer.Write();
        }

        static Color GetColor(COLDefinition.CallTypes Type, int colIndex, SFPalette palette)
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
            return new Color(fooColor.R, fooColor.G, fooColor.B);
        }
        static Color GetMaterialEntry(COLDefinition definition, SFPalette currentSFPalette)
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
            return new Color();
        }
    }
}
