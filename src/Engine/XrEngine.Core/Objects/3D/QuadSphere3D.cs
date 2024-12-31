using System.Numerics;

namespace XrEngine
{
    public class QuadSphere3D : Geometry3D, IGeneratedContent
    {
        protected Vector2 _patchSize;

        public QuadSphere3D()
            : this(1, 3)
        {
        }

        public QuadSphere3D(float radius, int levels)
        {
            Flags |= EngineObjectFlags.Readonly;
            Radius = radius;
            Levels = levels;
            Primitive = DrawPrimitive.Quad;
            Build();
        }


        public void Build()
        {
            // Generate initial cube quads
            (List<Vector3>, List<uint>) GenerateCubeQuads()
            {
                var vertices = new List<Vector3>();
                var indices = new List<uint>();

                Vector3[] data = [
                   Vector3.Normalize(new Vector3(-1, -1, -1)),
                   Vector3.Normalize( new Vector3(1, -1, -1)),
                   Vector3.Normalize( new Vector3(1, 1, -1)),
                   Vector3.Normalize( new Vector3(-1, 1, -1)),
                   Vector3.Normalize( new Vector3(-1, -1, 1)),
                   Vector3.Normalize( new Vector3(1, -1, 1)),
                   Vector3.Normalize( new Vector3(1, 1, 1)),
                   Vector3.Normalize( new Vector3(-1, 1, 1))
                ];

                vertices.AddRange(data);

                indices.AddRange([
                    0, 1, 2, 3, // Front
                    4, 5, 6, 7, // Back
                    0, 1, 5, 4, // Bottom
                    3, 2, 6, 7, // Top
                    0, 3, 7, 4, // Left
                    1, 2, 6, 5  // Right
                ]);

                return (vertices, indices);
            }

            // Subdivide quads and project onto sphere
            (List<Vector3>, List<uint>) SubdivideAndProject(List<Vector3> vertices, List<uint> indices)
            {
                var newVertices = new List<Vector3>();
                var newIndices = new List<uint>();

                for (int i = 0; i < indices.Count; i += 4)
                {
                    // Get the indices of the quad
                    uint i0 = indices[i];
                    uint i1 = indices[i + 1];
                    uint i2 = indices[i + 2];
                    uint i3 = indices[i + 3];

                    // Get the vertices of the quad
                    var v0 = vertices[(int)i0];
                    var v1 = vertices[(int)i1];
                    var v2 = vertices[(int)i2];
                    var v3 = vertices[(int)i3];

                    // Calculate midpoints
                    var m01 = Vector3.Normalize((v0 + v1) * 0.5f);
                    var m12 = Vector3.Normalize((v1 + v2) * 0.5f);
                    var m23 = Vector3.Normalize((v2 + v3) * 0.5f);
                    var m30 = Vector3.Normalize((v3 + v0) * 0.5f);
                    var center = Vector3.Normalize((v0 + v1 + v2 + v3) * 0.25f);

                    // Add midpoints to vertex list
                    uint i_m01 = AddVertex(newVertices, m01);
                    uint i_m12 = AddVertex(newVertices, m12);
                    uint i_m23 = AddVertex(newVertices, m23);
                    uint i_m30 = AddVertex(newVertices, m30);
                    uint i_center = AddVertex(newVertices, center);

                    // Add original vertices to new vertex list
                    uint i_v0 = AddVertex(newVertices, v0);
                    uint i_v1 = AddVertex(newVertices, v1);
                    uint i_v2 = AddVertex(newVertices, v2);
                    uint i_v3 = AddVertex(newVertices, v3);

                    // Create new quads
                    newIndices.AddRange(FixQuadWinding(newVertices, i_v0, i_m01, i_center, i_m30));
                    newIndices.AddRange(FixQuadWinding(newVertices, i_m01, i_v1, i_m12, i_center));
                    newIndices.AddRange(FixQuadWinding(newVertices, i_center, i_m12, i_v2, i_m23));
                    newIndices.AddRange(FixQuadWinding(newVertices, i_m30, i_center, i_m23, i_v3));
                }

                return (newVertices, newIndices);
            }

            // Ensure correct winding order
            uint[] FixQuadWinding(List<Vector3> vertices, uint i0, uint i1, uint i2, uint i3)
            {
                var v0 = vertices[(int)i0];
                var v1 = vertices[(int)i1];
                var v2 = vertices[(int)i2];

                var normal = Vector3.Cross(v1 - v0, v2 - v0);
                var centerToV0 = Vector3.Normalize(v0);

                if (Vector3.Dot(normal, centerToV0) < 0)
                    return [i0, i3, i2, i1];

                return [i0, i1, i2, i3];
            }

            // Add vertex to list if not already present
            uint AddVertex(List<Vector3> vertices, Vector3 vertex)
            {
                int index = vertices.IndexOf(vertex);
                if (index == -1)
                {
                    vertices.Add(vertex);
                    return (uint)(vertices.Count - 1);
                }
                return (uint)index;
            }


            (List<Vector3>, List<uint>) GenerateSphere(int levels)
            {
                var (vertices, indices) = GenerateCubeQuads();

                for (int i = 0; i < levels; i++)
                    (vertices, indices) = SubdivideAndProject(vertices, indices);

                return (vertices, indices);
            }

            Vector4 ComputeTangent(Vector3 pos)
            {
                var normal = Vector3.Normalize(pos);

                var arbitrary = Math.Abs(normal.Y) > 0.999f ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);

                var tangent = Vector3.Normalize(Vector3.Cross(arbitrary, normal));

                var bitangent = Vector3.Cross(normal, tangent);

                var w = Vector3.Dot(Vector3.Cross(tangent, bitangent), normal) > 0.0f ? 1.0f : -1.0f;

                return new Vector4(tangent, w);
            }

            // Calculate UV mapping
            Vector2 CalculateUV(Vector3 vertex)
            {
                var normal = Vector3.Normalize(vertex);

                // Longitude: range [-PI, PI]
                float longitude = (float)Math.Atan2(-normal.Z, normal.X);

                // Latitude: range [-PI/2, PI/2]
                float latitude = (float)Math.Asin(normal.Y);

                // Convert longitude to U in [0, 1]
                float u = (longitude / (2.0f * (float)Math.PI)) + 0.5f;

                // Convert latitude to V in [0, 1], Y-flipped
                float v = 1.0f - ((latitude / (float)Math.PI) + 0.5f);

                return new Vector2(u, v);
            }

            bool CrossesSeam(IEnumerable<float> us)
            {
                float maxU = us.Max();
                float minU = us.Min();
                return (maxU - minU) > 0.5f;
            }

            // Build sphere
            var (sphereVertices, sphereIndices) = GenerateSphere(Levels);

            var finalVertices = new List<VertexData>();
            var finalIndices = new List<uint>();
            var vertexMap = new Dictionary<(Vector3, Vector2), uint>();

            for (int i = 0; i < sphereIndices.Count; i += 4)
            {
                uint[] quad = [sphereIndices[i], sphereIndices[i + 1], sphereIndices[i + 2], sphereIndices[i + 3]];
                var positions = quad.Select(idx => sphereVertices[(int)idx]).ToArray();
                var uvs = positions.Select(CalculateUV).ToArray();

                // Check for seam crossing
                bool crossesSeam = CrossesSeam(uvs.Select(uv => uv.X));

                uint[] quadIndices = new uint[4];

                for (int j = 0; j < 4; j++)
                {
                    var position = positions[j] * Radius;
                    var uv = uvs[j];

                    if (crossesSeam && uv.X < 0.25f)
                        uv.X += 1.0f;

                    var key = (position, uv);

                    if (!vertexMap.TryGetValue(key, out uint index))
                    {

                        var vertexData = new VertexData
                        {
                            Pos = position,
                            Normal = Vector3.Normalize(positions[j]),
                            Tangent = ComputeTangent(position),
                            UV = uv
                        };
                        finalVertices.Add(vertexData);
                        index = (uint)(finalVertices.Count - 1);
                        vertexMap[key] = index;
                    }

                    quadIndices[j] = index;
                }

                finalIndices.AddRange(quadIndices);
            }

            Vertices = finalVertices.ToArray();
            Indices = finalIndices.ToArray();

            ActiveComponents |= VertexComponent.Normal | VertexComponent.UV0 | VertexComponent.Tangent;
        }

        public int Levels { get; set; }

        public float Radius { get; set; }

        public Vector3 Center { get; set; }

        public Vector2 PatchSize => _patchSize;
    }
}