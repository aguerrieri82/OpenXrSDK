using XrEngine;

namespace XrSamples.Graffiti.Objects
{
    public class SprayRays : TriangleMesh
    {
        public SprayRays(Geometry3D geometry)
        {
            Materials.Add(new SprayMaterial());

            var vertices = new VertexData[geometry.Vertices.Length * 2];

            for (var i = 0; i < geometry.Vertices.Length; i++)
            {
                vertices[(i * 2)] = geometry.Vertices[i];
                vertices[(i * 2) + 1] = geometry.Vertices[i];
            }

            var newGeo = new Geometry3D
            {
                Vertices = vertices,
                Primitive = DrawPrimitive.Line,
                ActiveComponents = VertexComponent.Position
            };

            CompressionMode = MeshCompressionMode.Never;
            Geometry = newGeo;
        }
    }
}
