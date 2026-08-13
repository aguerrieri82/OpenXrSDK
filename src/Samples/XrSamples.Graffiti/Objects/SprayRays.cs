using XrEngine;

namespace XrSamples.Graffiti.Objects
{
    public class SprayRays : TriangleMesh
    {
        public SprayRays(Geometry3D geometry)
        {
            Materials.Add(new SprayMaterial());

            var vertices = new VertexData[geometry.VerticesArray.Length * 2];

            for (var i = 0; i < geometry.VerticesArray.Length; i++)
            {
                vertices[(i * 2)] = geometry.VerticesArray[i];
                vertices[(i * 2) + 1] = geometry.VerticesArray[i];
            }

            var newGeo = new SimpleGeometry3D();
            newGeo.Vertices = vertices;
            newGeo.Primitive = DrawPrimitive.Line;
            newGeo.ActiveComponents = VertexComponent.Position;
            Geometry = newGeo;
        }
    }
}
