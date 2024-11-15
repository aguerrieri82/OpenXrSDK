using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BasicMaterial : ShaderMaterial, IColorSource
    {
        static readonly BasicShader SHADER;

        #region BasicShader

        class BasicShader : StandardVertexShader
        {
            string? _lightHash;

            public override void UpdateShader(ShaderUpdateBuilder bld)
            {
                bld.ExecuteAction((ctx, up) =>
                {
                    foreach (var light in bld.Context.Lights!)
                    {
                        if (light is AmbientLight ambient)
                            up.SetUniform("light.ambient", (Vector3)ambient.Color * ambient.Intensity);
                        
                        else if (light is ImageLight img)
                            up.SetUniform("light.ambient", (Vector3)img.Color * img.Intensity * 0.1f);

                        else if (light is PointLight point)
                        {
                            up.SetUniform("light.diffuse", (Vector3)point.Color * point.Intensity);
                            up.SetUniform("light.position", point.WorldPosition);
                            up.SetUniform("light.specular", (Vector3)point.Specular);
                        }
                    }

                    _lightHash = ctx.LightsHash!;
                });

                base.UpdateShader(bld);
            }

            public override bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                if (_lightHash != ctx.LightsHash)
                    return true;
                return base.NeedUpdateShader(ctx);
            }
        }

        #endregion

        static BasicMaterial()
        {
            SHADER = new BasicShader
            {
                FragmentSourceName = "basic.frag",
                IsLit = true
            };
        }

        public BasicMaterial()
        {
            Specular.Rgb(0.5f);
            Ambient = Color.White;
            Shininess = 32f;
            Shader = SHADER;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<BasicMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<BasicMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (DiffuseTexture != null)
            {
                bld.AddFeature("TEXTURE");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.LoadTexture(DiffuseTexture, 0);
                });
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("material.ambient", (Vector3)Ambient);
                up.SetUniform("material.diffuse", Color);
                up.SetUniform("material.specular", (Vector3)Specular);
                up.SetUniform("material.shininess", Shininess);
            });

        }

        public Texture2D? DiffuseTexture { get; set; }

        public Color Color { get; set; }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}
