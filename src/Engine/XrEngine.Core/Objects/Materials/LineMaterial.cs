using System.Diagnostics;

namespace XrEngine
{
    public class LineMaterial : ShaderMaterial, ILineMaterial
    {
        static readonly Shader SHADER;

        class LineVertexShader : Shader, IShaderHandler
        {
            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                return true;
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {
                var stage = bld.Context.Stage;

                if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Shader)
                {
                    bld.LoadBuffer((ctx) =>
                    {
                        Debug.Assert(ctx.PassCamera != null);

                        var result = new CameraUniforms
                        {
                            ViewProj = ctx.PassCamera.ViewProjection,
                            Position = ctx.PassCamera.WorldPosition,
                            NearPlane = ctx.PassCamera.Near,
                            FarPlane = ctx.PassCamera.Far,
                        };

                        return (CameraUniforms?)result;

                    }, 0, BufferStore.Shader);
                }
            }
        }

        static LineMaterial()
        {
            SHADER = new LineVertexShader
            {
                FragmentSourceName = "line.frag",
                VertexSourceName = "line.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public LineMaterial()
            : base()
        {
            _shader = SHADER;
            LineWidth = 1;
        }


        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.SetUniform("uWorldMatrix", (ctx) => ctx.Model!.WorldMatrix);

        }

        public float LineWidth { get; set; }
    }
}
