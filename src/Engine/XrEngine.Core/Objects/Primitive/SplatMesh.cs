using System.Numerics;
using XrEngine.Objects.Materials;
using XrMath;

namespace XrEngine
{
    public class SplatMesh : Object3D, IVertexSource<Vector2, uint>, ILocalBounds
    {
        static readonly Vector2[] _vertices =
        [
            new(-1, -1),
            new( 1, -1),
            new( 1,  1),
            new(-1,  1),
        ];

        static readonly uint[] _indices =
        [
            0, 1, 2,
            0, 2, 3,
        ];

        private Bounds3 _localBounds;
        private readonly ShaderMaterial _depthMaterial;

        public SplatMesh()
        {
            _depthMaterial = new SplatMaterial()
            {
                Shader = new Shader
                {
                    FragmentSourceName = "empty.frag",
                    VertexSourceName = "splats.vert",
                    Resolver = str => Embedded.GetString(str),
                },
                IsEnabled = true,
                Radius = 0.01f,
                UseCameraFacing = true,
                UseDistanceScale = false,
                WriteColor = false,
                WriteDepth = true,
                Priority = 0
            };

            _depthMaterial.Attach(this);

            Material = new SplatMaterial()
            {
                Priority = 1,
                WriteDepth = false,
            };

            Material.Attach(this);

            Splats = [];
            ActiveComponents = VertexComponent.Position;
            BoundUpdateMode = UpdateMode.Automatic;
        }

        public SplatMesh(SplatData[] data)
            : this()
        {
            Splats = data;
        }

        public void NotifyLoaded()
        {
        }

        public override void UpdateBounds(bool force = false)
        {
            var builder = new Bounds3Builder();

            builder.Add(Splats.Select(a => a.Position));
            _localBounds = builder.Result;

            base.UpdateBounds(force);
        }

        public SplatData[] Splats { get; set; }

        public SplatMaterial Material { get; }

        public VertexComponent ActiveComponents { get; set; }

        public int RenderPriority { get; set; }

        public Bounds3 LocalBounds => _localBounds;

        public UpdateMode BoundUpdateMode { get; set; }

        #region IVertexSource

        EngineObject IVertexSource.Host => this!;

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Triangle;

        uint[] IVertexSource<Vector2, uint>.Indices => _indices;

        Vector2[] IVertexSource<Vector2, uint>.Vertices => _vertices;

        IReadOnlyList<Material> IVertexSource.Materials => [_depthMaterial, Material];

        int IVertexSource.InstanceCount => Splats.Length;

        #endregion
    }
}
