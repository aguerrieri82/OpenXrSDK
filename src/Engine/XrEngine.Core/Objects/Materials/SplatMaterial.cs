using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;


namespace XrEngine.Objects.Materials
{
    public partial class SplatMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static SplatMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "splats.frag",
                VertexSourceName = "splats.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public SplatMaterial()
        {
            _shader = SHADER;

            FadeStart = 0.3f;
            Radius = 0.008f;
            MaxRadius = 0.0215f;
            DistanceScale = 0.004f;
            DepthBias = 0.60f;
            UseCameraFacing = true;
            UseDistanceScale = true;

            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.LoadBufferArray(ctx =>
            {
                if (ctx.Model is not SplatMesh mesh)
                    return null;

                var curVersion = ctx.Model!.Version;

                if (ctx.CurrentBuffer!.Version == curVersion)
                    return null;

                ctx.CurrentBuffer!.Version = curVersion;

                return mesh.Splats;

            }, 18, BufferStore.Model, false);


            base.UpdateShaderModel(bld);
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (UseCameraFacing)
            {
                bld.AddFeature("CAMERA_FACING");

                bld.ExecuteAction((ctx, up) =>
                {
                    var viewInv = ctx.PassCamera!.ViewInverse;

                    var cameraRight = new Vector3(viewInv.M11, viewInv.M12, viewInv.M13);
                    var cameraUp = new Vector3(viewInv.M21, viewInv.M22, viewInv.M23);

                    up.SetUniform("uCameraRight", cameraRight);
                    up.SetUniform("uCameraUp", cameraUp);
                });
            }

            if (UseDistanceScale)
            {
                bld.AddFeature("DISTANCE_SCALE");

                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uView", ctx.PassCamera!.View);
                    up.SetUniform("uSplatDistanceScale", DistanceScale);
                    up.SetUniform("uSplatMinRadius", Radius);
                    up.SetUniform("uSplatMaxRadius", MaxRadius);
                });
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uFadeStart", FadeStart);
                up.SetUniform("uSplatRadius", Radius);
                up.SetUniform("uViewProj", ctx.PassCamera!.ViewProjection);
                up.SetUniform("uSplatDepthBias", LogDepthBias(DepthBias));
            });

            base.UpdateShaderMaterial(bld);
        }

        static float LogDepthBias(float t) => t <= 0 ? 0f : 0.00001f * MathF.Pow(0.005f / 0.00001f, t);



        [Range(0,1, 0.01f)]
        public float FadeStart { get; set; }

        [Range(0, 0.1f, 0.0005f)]
        public float Radius { get; set; }

        [Range(0, 0.1f, 0.0005f)]
        public float MaxRadius { get; set; }

        [Range(0, 0.1f, 0.0005f)]
        public float DistanceScale { get; set; }

        [Notify(ChangeType.Render)]
        public partial bool UseCameraFacing { get; set; }

        [Notify(ChangeType.Render)]
        public partial bool UseDistanceScale { get; set; }

        [Range(0, 1f, 0.01f)]
        public float DepthBias { get; set; }
    }
}
