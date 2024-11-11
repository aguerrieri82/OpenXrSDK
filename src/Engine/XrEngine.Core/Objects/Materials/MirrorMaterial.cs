using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public enum MirrorMode
    {
        Planar,
        Full,
        Fixed
    }

    public class MirrorMaterial : ShaderMaterial
    {
        static readonly StandardVertexShader SHADER;

        static MirrorMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "mirror.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public MirrorMaterial()
        {
            _shader = SHADER;
            TextureSize = 1024;
        }

        public override void Attach(EngineObject host)
        {
            base.Attach(host);

            if (!host.TryComponent<PlanarReflection>(out _))
                host.AddComponent(new PlanarReflection(TextureSize, PlanarReflectionMode.Full)
                {
                    AutoAdjustFov = true,
                    UseClipPlane = true,
                    MaterialOverride = new BasicMaterial()
                });
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var planar = bld.Context.Model!.Components<PlanarReflection>().FirstOrDefault();
            
            Debug.Assert(planar != null);

            bld.AddFeature("PLANAR_REFLECTION");

            if (DoubleSided)
                bld.AddFeature("DOUBLE_SIDED");

            if (PlanarReflection.IsMultiView)
                bld.AddFeature("PLANAR_REFLECTION_MV");

            bld.AddFeature("PURE_REFLECTION");

            bld.AddFeature($"MIRROR_MODE {(int)Mode}");

            bld.ExecuteAction((ctx, up) =>
            {
                if (planar.Texture != null)
                    up.LoadTexture(planar.Texture, 7);

                if (PlanarReflection.IsMultiView)
                {
                    if (planar.ReflectionCamera.Eyes != null)
                    {
                        up.SetUniform("uReflectMatrix[0]", planar.ReflectionCamera.Eyes[0].ViewProj);
                        up.SetUniform("uReflectMatrix[1]", planar.ReflectionCamera.Eyes[1].ViewProj);
                    }
                }
                else
                    up.SetUniform("uReflectMatrix", planar.ReflectionCamera.ViewProjection);

                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                
            });

            base.UpdateShader(bld);
        }

        public uint TextureSize { get; set; } 

        public MirrorMode Mode { get; set; }
    }
}
