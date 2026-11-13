using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class HitCross : TriangleMesh
    {
        public HitCross()
        {
            var builder = new MeshBuilder();

            builder.AddCube(Vector3.Zero, new Vector3(0.08f, 0.005f, 0.001f));
            builder.AddCube(Vector3.Zero, new Vector3(0.005f, 0.08f, 0.001f));

            Geometry = builder.ToGeometry(Geometry);

            Materials.Add(new ColorMaterial(new Color(0, 0.4f, 1))
            {
                UseDepth = false,
                WriteDepth = false,
                Priority = 12
            });

            Name = "WallCross";
            IsVisible = false;
        }
    }
}
