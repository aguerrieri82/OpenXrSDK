using System.Numerics;

namespace XrEngine
{
    public class Sphere3D : Geometry3D
    {
        public Sphere3D()
            : this(1f, 50, 50)
        {

        }

        public Sphere3D(float radius, uint latSegments, uint lonSegments)
        {
            Radius = radius;
            Build(latSegments, lonSegments);
        }

        public void Build(uint horizontal, uint vertical)
        {
            var indices = new List<uint>();
            var vertices = new List<VertexData>();

            for (int y = 0; y <= vertical; y++)
            {
                float v = 1.0f - (float)y / vertical;
                float phi = v * (float)MathF.PI;

                for (int x = 0; x <= horizontal; x++)
                {
                    float u = (float)x / horizontal;
                    float theta = u * 2.0f * (float)MathF.PI;

                    float sinTheta = (float)MathF.Sin(theta);
                    float cosTheta = (float)MathF.Cos(theta);
                    float sinPhi = (float)MathF.Sin(phi);
                    float cosPhi = (float)MathF.Cos(phi);

                    var normal = new Vector3(cosTheta * sinPhi, cosPhi, sinTheta * sinPhi);
                    var tangent = new Vector3(-sinTheta, 0.0f, cosTheta);

                    vertices.Add(new VertexData
                    {
                        Pos = Radius * normal,
                        Normal = normal,
                        UV = new Vector2(u, v),
                        Tangent = new Vector4(tangent, 1.0f)
                    });
                }
            }

            for (int y = 0; y < vertical; y++)
            {
                for (int x = 0; x < horizontal; x++)
                {
                    var index0 = (uint)(y * (horizontal + 1) + x);
                    var index1 = (uint)(index0 + 1);
                    var index2 = (uint)((y + 1) * (horizontal + 1) + x);
                    var index3 = (uint)(index2 + 1);

                    indices.Add(index0);
                    indices.Add(index1);
                    indices.Add(index2);

                    indices.Add(index2);
                    indices.Add(index1);
                    indices.Add(index3);
                }
            }

            Vertices = vertices.ToArray();
            Indices = indices.ToArray();

            ActiveComponents |= VertexComponent.Normal | VertexComponent.UV0 | VertexComponent.Tangent;
        }

        public float Radius;


        public static readonly Sphere3D Instance = new Sphere3D();
    }
}
