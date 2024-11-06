using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class FilledSphere3D : Geometry3D, IGeneratedContent
    {
        public FilledSphere3D()
        {
            Flags |= EngineObjectFlags.Readonly;
        }

        public FilledSphere3D(float radius, uint subs, float section = 1)
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

            for (int lat = 0; lat <= Subs; lat++)
            {
                float theta = lat * MathF.PI / Subs;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                for (int lon = 0; lon <= Subs; lon++)
                {
                    float phi = lon * 2 * MathF.PI * Section / Subs;
                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    float x = sinTheta * cosPhi;
                    float y = cosTheta;
                    float z = sinTheta * sinPhi;

                    var tangent = new Vector3(-sinPhi, 0, cosPhi).Normalize();

                    vertices.Add(new VertexData
                    {
                        Pos = new Vector3(x, y, z) * Radius,
                    });
                }
            }

            vertices.Add(new VertexData
            {
                Pos = Vector3.Zero
            });

            var zero = (uint)vertices.Count - 1;

            var indices = new List<uint>();

            for (uint lat = 0; lat < Subs; lat++)
            {
                for (uint lon = 0; lon < Subs; lon++)
                {
                    uint first = lat * (Subs + 1) + lon;

                    // Triangle 1
                    indices.Add(zero);
                    indices.Add(first + 1);
                    indices.Add(first);

                    uint second = (lat + 1) * (Subs + 1) + lon;

                    // Triangle 1
                    indices.Add(zero);
                    indices.Add(first);
                    indices.Add(second);

                }
            }

            _vertices = vertices.ToArray();
            _indices = indices.ToArray();

            this.ComputeNormals();

            ActiveComponents |= VertexComponent.Normal;
        }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Section { get; set; }


        public static readonly FilledSphere3D Default = new(1f, 30);


        public static readonly FilledSphere3D SemiSphere = new(1f, 30, 0.5f);
    }
}
