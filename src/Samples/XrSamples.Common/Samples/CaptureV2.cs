using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenXr;
using XrMath;


namespace XrSamples
{
    public partial class OverlayTextureV2Effect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static OverlayTextureV2Effect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "texture.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString<Material>(str),
                IsLit = false,
                Priority = 1
            };
        }

        public OverlayTextureV2Effect()
        {
            Shader = SHADER;
            UseDepth = false;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("CLAMP");

            if (Texture != null)
            {
                if (Texture.Type == TextureType.External)
                {
                    bld.AddExtension("GL_OES_EGL_image_external_essl3");
                    bld.AddFeature("EXTERNAL");
                }

                if (Texture.Transform != null)
                    bld.AddFeature("USE_TRANSFORM");
            }

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture?.Transform != null)
                    up.SetUniform("uUvTransform", Texture.Transform.Value);

                up.SetUniform("uColor", Color.White);
            });

            bld.PrepareTexture(Texture);

            bld.LoadTextureFixSrgb(() => Texture, 0);
        }

        [Notify(ChangeType.Render)]
        public partial Texture2D? Texture { get; set; }
    }

    public static partial class SampleScenes
    {
        [Sample("Capture2")]
        public static XrEngineAppBuilder CreateCaptureV2(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var controller = scene.AddComponent<CameraController>();

            var leftPos = Vector3.Zero;

            var effect = new OverlayTextureV2Effect();

            var mesh = scene.AddChild(new VirtualMesh(effect, 3));

            controller.StartCamera(OculusCameras.Left);

            controller.StartCamera(OculusCameras.Right);

            scene.AddBehavior((self, ctx) =>
            {
                var leftStatus = controller.GetCameraStatus(OculusCameras.Left);
                var rightStatus = controller.GetCameraStatus(OculusCameras.Right);

                if (leftStatus.IsActive && rightStatus.IsActive)
                {
                    mesh.IsVisible = true;

                    var texture = leftStatus.Texture;

                    Debug.Assert(ctx.Camera?.Eyes != null && texture != null);

                    texture.Transform = controller.GetUvTransform(OculusCameras.Left,
                        ctx.Camera.Eyes[0].View,
                        ctx.Camera.Eyes[0].Projection);
                    effect.Texture = texture;
                }
            });

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }
    }
}
