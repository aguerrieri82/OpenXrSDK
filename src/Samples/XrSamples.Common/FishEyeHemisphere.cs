using System.Numerics;

namespace XrEngine
{
    public class FishEyeHemisphere : Geometry3D, IGeneratedContent
    {
        /// <summary>
        /// Hemisphere radius in local geometry units.
        /// </summary>
        public float Radius { get; set; } = 1.0f;

        /// <summary>
        /// Angular subdivisions around the hemisphere.
        /// </summary>
        public int Slices { get; set; } = 96;

        /// <summary>
        /// Radial subdivisions from fisheye center to edge.
        /// </summary>
        public int Stacks { get; set; } = 48;

        /// <summary>
        /// Center of the fisheye circle in full texture UV coordinates.
        /// Left SBS usually:  (0.25, 0.5)
        /// Right SBS usually: (0.75, 0.5)
        /// </summary>
        public Vector2 TexCenter { get; set; } = new(0.5f, 0.5f);

        /// <summary>
        /// Diameter of the fisheye circle in full texture UV coordinates.
        /// For a circle filling one half of an SBS frame: (0.5, 1.0)
        /// </summary>
        public Vector2 TexDiameter { get; set; } = new(1f, 1.0f);

        /// <summary>
        /// Generate inward normals and inward triangle winding.
        /// </summary>
        public bool Inward { get; set; } = true;

        public FishEyeHemisphere()
        {
            Build();
        }

        public FishEyeHemisphere(
            float radius,
            int slices,
            int stacks,
            Vector2 texCenter,
            Vector2 texDiameter,
            bool inward = true)
        {
            Radius = radius;
            Slices = slices;
            Stacks = stacks;
            TexCenter = texCenter;
            TexDiameter = texDiameter;
            Inward = inward;

            Build();
        }

        public void Build()
        {
            int slices = Math.Max(3, Slices);
            int stacks = Math.Max(1, Stacks);

            var vertices = new VertexData[(slices + 1) * (stacks + 1)];
            var indices = new uint[slices * stacks * 6];

            int vi = 0;

            for (int sy = 0; sy <= stacks; sy++)
            {
                float fy = sy / (float)stacks;

                // 0      = fisheye center direction
                // PI / 2 = hemisphere edge
                float alpha = fy * MathF.PI * 0.5f;

                float sinA = MathF.Sin(alpha);
                float cosA = MathF.Cos(alpha);

                // Your convention:
                // 180° fisheye => FOV = PI
                // hemisphere edge alpha = PI / 2
                // UV radius at edge = 0.5
                float fishR = alpha / MathF.PI;

                for (int sx = 0; sx <= slices; sx++)
                {
                    float fx = sx / (float)slices;
                    float beta = fx * MathF.PI * 2.0f;

                    float cosB = MathF.Cos(beta);
                    float sinB = MathF.Sin(beta);

                    // Matches shader convention:
                    // lng = atan(-p.y, -p.x)
                    var pos = new Vector3(
                        -Radius * sinA * cosB,
                        -Radius * sinA * sinB,
                         Radius * cosA
                    );

                    var normal = Vector3.Normalize(pos);

                    if (Inward)
                        normal = -normal;

                    var uv = new Vector2(
                        TexCenter.X + cosB * fishR * TexDiameter.X,
                        TexCenter.Y + sinB * fishR * TexDiameter.Y
                    );

                    vertices[vi++] = new VertexData
                    {
                        Pos = pos,
                        Normal = normal,
                        UV = uv
                    };
                }
            }

            int ii = 0;
            int stride = slices + 1;

            for (int sy = 0; sy < stacks; sy++)
            {
                for (int sx = 0; sx < slices; sx++)
                {
                    uint i0 = (uint)(sy * stride + sx);
                    uint i1 = i0 + 1;
                    uint i2 = i0 + (uint)stride;
                    uint i3 = i2 + 1;

                    if (Inward)
                    {
                        indices[ii++] = i0;
                        indices[ii++] = i2;
                        indices[ii++] = i1;

                        indices[ii++] = i1;
                        indices[ii++] = i2;
                        indices[ii++] = i3;
                    }
                    else
                    {
                        indices[ii++] = i0;
                        indices[ii++] = i1;
                        indices[ii++] = i2;

                        indices[ii++] = i1;
                        indices[ii++] = i3;
                        indices[ii++] = i2;
                    }
                }
            }

            Vertices = vertices;
            Indices = indices;

            Primitive = DrawPrimitive.Triangle;

            ActiveComponents =
                VertexComponent.Position |
                VertexComponent.Normal |
                VertexComponent.UV0;

        }
    }
}