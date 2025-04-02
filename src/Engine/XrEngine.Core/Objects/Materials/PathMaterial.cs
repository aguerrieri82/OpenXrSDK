using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PathMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static PathMaterial()
        {
            SHADER = new CameraOnlyVertexShader
            {
                VertexSourceName = "path.vert",
                FragmentSourceName = "color.frag",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public PathMaterial()
        {
            _shader = SHADER;
            Points = [];
        }

        public PathMaterial(Color color)
            : this()
        {
            Color = color;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<PathMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<PathMaterial>(this);
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uWordMatrix", ctx.Model!.WorldMatrix);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature($"POINT_COUNT {Math.Max(1, Points.Length)}");
            if (UseVertexColor)
                bld.AddFeature($"USE_VERTEX_COLOR");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uPoints", Points);
                up.SetUniform("uColor", Color);
            });
        }

        public Vector3[] Points { get; set; }

        public Color Color { get; set; }

        public bool UseVertexColor { get; set; }
    }
}
