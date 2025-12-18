using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class IsoSphere3D : Geometry3D, IGeneratedContent
    {
        public IsoSphere3D()
            : this(1, 3)
        {

        }

        public IsoSphere3D(float radius, uint levels)

        {
            Flags |= EngineObjectFlags.Readonly;
            Radius = radius;
            Levels = levels;
            Build();
        }

        public void Build()
        {
            float X = 0.525731112119133606f;
            float Z = 0.850650808352039932f;

            List<Vector3> vertices = new List<Vector3>(MathUtils.Vector3FromArray(new float[] {
                 -X, 0.0f, Z,  X, 0.0f, Z,  -X, 0.0f, -Z,  X, 0.0f, -Z,
                0.0f, Z, X,  0.0f, Z, -X,  0.0f, -Z, X,  0.0f, -Z, -X,
                Z, X, 0.0f,  -Z, X, 0.0f,  Z, -X, 0.0f,  -Z, -X, 0.0f
            }));

            uint[] indices0 = new uint[]
            {
                0,4,1,   0,9,4,   9,5,4,  4,5,8,  4,8,1,
                8,10,1,  8,3,10,  5,3,8,  5,2,3,  2,7,3,
                7,10,3,  7,6,10,  7,11,6, 11,0,6, 0,1,6,
                6,1,10,  9,0,11,  9,11,2, 9,2,5,  7,2,11
            };

            List<IList<uint>> indicesLevels = new List<IList<uint>>
            {
                indices0
            };

            uint VertexForEdge(Dictionary<long, uint> map, uint first, uint second)
            {
                if (first > second)
                    (first, second) = (second, first);

                long key = first | (((long)second) << 32);

                if (!map.TryGetValue(key, out uint edge))
                {
                    edge = (uint)vertices.Count;
                    map[key] = edge;
                    vertices.Add((vertices[(int)first] + vertices[(int)second]).Normalize());
                }

                return edge;
            }

            void Subdivide()
            {
                Dictionary<long, uint> edgeMap = new Dictionary<long, uint>();
                IList<uint> indices = indicesLevels[^1];
                List<uint> refinedIndices = new List<uint>();
                indicesLevels.Add(refinedIndices);

                int end = indices.Count;

                for (int i = 0; i < end; i += 3)
                {
                    Span<uint> mid = new Span<uint>(new uint[3]);
                    Span<uint> idx = new Span<uint>(new uint[3]);

                    for (int k = 0; k < 3; k++)
                        idx[k] = indices[i + k];

                    for (int k = 0; k < 3; k++)
                        mid[k] = VertexForEdge(edgeMap, idx[k], idx[(k + 1) % 3]);

                    refinedIndices.Add(idx[0]); refinedIndices.Add(mid[0]); refinedIndices.Add(mid[2]);
                    refinedIndices.Add(idx[1]); refinedIndices.Add(mid[1]); refinedIndices.Add(mid[0]);
                    refinedIndices.Add(idx[2]); refinedIndices.Add(mid[2]); refinedIndices.Add(mid[1]);
                    refinedIndices.Add(mid[0]); refinedIndices.Add(mid[1]); refinedIndices.Add(mid[2]);
                }
            }

            while (indicesLevels.Count < Levels)
                Subdivide();

            Indices = indicesLevels[^1].ToArray();
            Vertices = new VertexData[vertices.Count];


            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 normal = vertices[i].Normalize();

                Vector3 up = Vector3.UnitY;
                Vector3 test = Vector3.Cross(normal, up);
                if (float.IsNaN(test.X) || test.Length() < 1e-10)
                    up = Vector3.UnitZ;

                Quaternion tangSpace = Quaternion.Normalize(MathUtils.QuatFromForwardUp(normal, up));

                Spherical spherical = Spherical.FromCartesian(normal * Radius);

                Vertices[i] = new VertexData
                {
                    Pos = vertices[i] * Radius,
                    Normal = normal,
                    //Tangent = tangSpace,
                    UV = new Vector2(
                        (spherical.Pol + MathF.PI) / (MathF.PI * 2),
                        spherical.Azm / (MathF.PI))
                };
            }

            this.EnsureCCW();


            ActiveComponents |= VertexComponent.Normal | VertexComponent.UV0;
        }

        public uint Levels { get; set; }

        public float Radius { get; set; }


        public static readonly IsoSphere3D Default = new(1f, 3);
    }
}
