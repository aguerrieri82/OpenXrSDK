using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Reconstruct
{
    public class GridMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        public class GridMaterialShader : StandardVertexShader
        {
            public GridMaterialShader()
            {
                FragmentSourceName = "grid.frag";
                VertexSourceName = "grid.vert";
                GeometrySourceName = "grid_cull.geom";
                Resolver = str => Embedded.GetString<GridMaterial>(str);
            }
        }

        static GridMaterial()
        {
            SHADER = new GridMaterialShader();
        }

        public GridMaterial()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var model = ctx.Model!;

                up.SetUniform("uWorldMatrix", model.WorldMatrix);

                if (model.TryComponent<CaptureFrame>(out var cap))
                {
                    var captureWorld = cap.Meta!.CameraView.Invert();

                    var captureCameraPos = captureWorld.Translation.Transform(model.WorldMatrix);

                    var captureCameraForward = Vector3.TransformNormal(
                        new Vector3(0, 0, -1),
                        captureWorld
                    );

                    captureCameraForward = Vector3.TransformNormal(
                        captureCameraForward,
                        model.WorldMatrix
                    );

                    captureCameraForward = Vector3.Normalize(captureCameraForward);

                    var captureCameraRight = Vector3.TransformNormal(
                        new Vector3(1, 0, 0),
                        captureWorld
                    );

                    captureCameraRight = Vector3.TransformNormal(
                        captureCameraRight,
                        model.WorldMatrix
                    );

                    captureCameraRight = Vector3.Normalize(captureCameraRight);

                    up.SetUniform("uCaptureCameraPos", captureCameraPos);
                    up.SetUniform("uCaptureCameraForward", captureCameraForward);
                    up.SetUniform("uCaptureCameraRight", captureCameraRight);
                }
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCullInvalidUv", CullInvalidUv ? 1 : 0);
                up.SetUniform("uCullLongEdge", CullLongEdge ? 1 : 0);
                up.SetUniform("uCullLateralFaces", CullLateralFaces ? 1 : 0);
                up.SetUniform("uCullDistance", CullDistance ? 1 : 0);
                up.SetUniform("uShowRejected", ShowRejected ? 1 : 0);

                up.SetUniform("uMaxEdgeBase", MaxEdgeBase);
                up.SetUniform("uMaxEdgePerMeter", MaxEdgePerMeter);

                up.SetUniform("uMinFrontness", MinFrontness);
                up.SetUniform("uMaxCaptureDistance", MaxCaptureDistance);

                if (Texture != null)
                    up.LoadTexture(Texture, 0);
            });

            base.UpdateShaderMaterial(bld);
        }

        public static bool CullInvalidUv { get; set; } = true;

        public static bool CullLongEdge { get; set; } = true;

        public static bool CullLateralFaces { get; set; } = false;

        public static bool CullDistance { get; set; } = false;

        public static bool ShowRejected { get; set; } = false;

        [Range(0.005f, 0.20f, 0.001f)]
        public static float MaxEdgeBase { get; set; } = 0.04f;

        [Range(0.0f, 0.02f, 0.0005f)]
        public static float MaxEdgePerMeter { get; set; } = 0.0f;

        [Range(0.0f, 1.0f, 0.01f)]
        public static float MinFrontness { get; set; } = 0.25f;

        [Range(0.1f, 20.0f, 0.1f)]
        public static float MaxCaptureDistance { get; set; } = 4.0f;

        public Texture2D? Texture { get; set; }
    }
}
