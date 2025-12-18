using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Sphere3D : Geometry3D, IGeneratedContent
    {
        public Sphere3D()
        {
            Flags |= EngineObjectFlags.Readonly;
        }

        public Sphere3D(float radius, uint subs, float section = 1)
            : this()
        {
            Radius = radius;
            Subs = subs;
            Section = section;
            Build();
        }

        public void Build()
        {
            var vertices = new List<VertexData>();

            for (var lat = 0; lat <= Subs; lat++)
            {
                var theta = lat * MathF.PI / Subs;
                var sinTheta = MathF.Sin(theta);
                var cosTheta = MathF.Cos(theta);

                for (var lon = 0; lon <= Subs; lon++)
                {
                    var phi = lon * 2 * MathF.PI * Section / Subs;
                    var sinPhi = MathF.Sin(phi);
                    var cosPhi = MathF.Cos(phi);

                    var x = sinTheta * cosPhi;
                    var y = cosTheta;
                    var z = sinTheta * sinPhi;

                    var tangent = new Vector3(-sinPhi, 0, cosPhi).Normalize();

                    vertices.Add(new VertexData
                    {
                        Pos = new Vector3(x, y, z) * Radius,
                        Normal = new Vector3(x, y, z).Normalize(),
                        Tangent = new Vector4(tangent, 1),
                        UV = new Vector2((float)lon / Subs, (float)lat / Subs)
                    });
                }
            }

            var indices = new List<uint>();

            for (uint lat = 0; lat < Subs; lat++)
            {
                for (uint lon = 0; lon < Subs; lon++)
                {
                    var first = lat * (Subs + 1) + lon;
                    var second = first + Subs + 1;

                    // Triangle 1
                    indices.Add(first + 1);
                    indices.Add(second);
                    indices.Add(first);

                    // Triangle 2
                    indices.Add(first + 1);
                    indices.Add(second + 1);
                    indices.Add(second);
                }
            }

            _vertices = vertices.ToArray();
            _indices = indices.ToArray();

            ActiveComponents |= VertexComponent.Normal | VertexComponent.UV0 | VertexComponent.Tangent;
        }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Section { get; set; }


        public static readonly Sphere3D Default = new(1f, 30);


        public static readonly Sphere3D SemiSphere = new(1f, 30, 0.5f);
    }
}
