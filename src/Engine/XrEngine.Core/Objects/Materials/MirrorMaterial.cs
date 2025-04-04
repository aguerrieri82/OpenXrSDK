﻿using System.Diagnostics;

namespace XrEngine
{
    public enum MirrorMode
    {
        Planar,
        Full
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
                VaryByModel = true,
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

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("PLANAR_REFLECTION");
            bld.AddFeature("PURE_REFLECTION");

            bld.AddFeature($"MIRROR_MODE {(int)Mode}");

            if (DoubleSided)
                bld.AddFeature("DOUBLE_SIDED");

            if (PlanarReflection.IsMultiView)
                bld.AddFeature("PLANAR_REFLECTION_MV");

            if (bld.Context.UseInstanceDraw && _hosts.Count == 1)
            {
                bld.Context.Model = (Object3D)_hosts.First();
                UpdateShaderModel(bld);
            }

            base.UpdateShaderMaterial(bld);
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            var planar = bld.Context.Model!.Components<PlanarReflection>().FirstOrDefault();

            Debug.Assert(planar != null);

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

            });
        }



        public uint TextureSize { get; set; }

        public MirrorMode Mode { get; set; }
    }
}
