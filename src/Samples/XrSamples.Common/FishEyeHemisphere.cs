using System.Numerics;

namespace XrEngine
{
    public class FishEyeHemisphere : SimpleGeometry3D, IGeneratedContent
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
            var slices = Math.Max(3, Slices);
            var stacks = Math.Max(1, Stacks);

            var vertices = new VertexData[(slices + 1) * (stacks + 1)];
            var indices = new uint[slices * stacks * 6];

            var vi = 0;

            for (var sy = 0; sy <= stacks; sy++)
            {
                var fy = sy / (float)stacks;

                // 0      = fisheye center direction
                // PI / 2 = hemisphere edge
                var alpha = fy * MathF.PI * 0.5f;

                var sinA = MathF.Sin(alpha);
                var cosA = MathF.Cos(alpha);

                // Your convention:
                // 180° fisheye => FOV = PI
                // hemisphere edge alpha = PI / 2
                // UV radius at edge = 0.5
                var fishR = alpha / MathF.PI;

                for (var sx = 0; sx <= slices; sx++)
                {
                    var fx = sx / (float)slices;
                    var beta = fx * MathF.PI * 2.0f;

                    var cosB = MathF.Cos(beta);
                    var sinB = MathF.Sin(beta);

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

            var ii = 0;
            var stride = slices + 1;

            for (var sy = 0; sy < stacks; sy++)
            {
                for (var sx = 0; sx < slices; sx++)
                {
                    var i0 = (uint)(sy * stride + sx);
                    var i1 = i0 + 1;
                    var i2 = i0 + (uint)stride;
                    var i3 = i2 + 1;

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
            _indices = indices;

            Primitive = DrawPrimitive.Triangle;

            ActiveComponents =
                VertexComponent.Position |
                VertexComponent.Normal |
                VertexComponent.UV0;

        }
    }
}