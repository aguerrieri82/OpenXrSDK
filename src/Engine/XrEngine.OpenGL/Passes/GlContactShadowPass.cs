#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlContactShadowPass : GlBaseRenderPass, IContactShadowProvider
    {
        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct ContactShadowUniforms
        {
            [FieldOffset(0)]
            public Vector4 LightDirWorld;

            [FieldOffset(16)]
            public Vector2 ViewSize;

            [FieldOffset(24)]
            public float MaxDistance;

            [FieldOffset(28)]
            public float Thickness;

            [FieldOffset(32)]
            public float Strength;

            [FieldOffset(36)]
            public float StepCount;

            [FieldOffset(40)]
            public float DepthBias;

            [FieldOffset(44)]
            public float FadeDistance;
        }

        const int CONTACT_SHADOW_BUF = 17;
        const int DEPTH_TEX_SLOT = 0;
        const int MASK_TEX_SLOT = 0;

        protected readonly GlRenderPassTarget _passTarget;
        protected readonly GlSimpleProgram _contactProgram;
        protected readonly GlSimpleProgram _applyProgram;

        protected readonly GlBuffer<ContactShadowUniforms> _uniforms;

        protected readonly bool _isMultiView;
        protected readonly int _boundEye;


        public GlContactShadowPass(OpenGLRender renderer, int boundEye = -1, bool isMultiView = false)
            : base(renderer)
        {
            _boundEye = boundEye;
            _isMultiView = isMultiView;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                BoundEye = boundEye,
                DepthMode = TargetDepthMode.None,
                IsMultiView = isMultiView,
                UseMultiViewTarget = true
            };

            _contactProgram = new GlSimpleProgram(
                renderer.GL,
                "fullscreen.vert",
                "contact_shadow.frag",
                str => Embedded.GetString<Material>(str));


            _applyProgram = new GlSimpleProgram(
                renderer.GL,
                "fullscreen.vert",
                "contact_shadow_apply.frag",
                str => Embedded.GetString<Material>(str));

            _contactProgram.AddFeature("DEPTH_SAMPLES 4");

            if (isMultiView)
            {
                _contactProgram.AddExtension("GL_OVR_multiview2");
                _contactProgram.AddFeature("MULTI_VIEW");


                _applyProgram.AddExtension("GL_OVR_multiview2");
                _applyProgram.AddFeature("MULTI_VIEW");
            }

            _contactProgram.Build();
            _applyProgram.Build();


            _contactProgram.Build();
            _uniforms = new GlBuffer<ContactShadowUniforms>(_gl, BufferTargetARB.UniformBuffer);
        }

        public override void Render(RenderContext ctx)
        {
            var options = _renderer.Options.ContactShadow;

            if (!options.Use)
                return;

            if (_renderer.RenderTarget is not GlMultiViewRenderTarget && _isMultiView)
                return;

            var camera = _renderer.UpdateContext.PassCamera!;

            var light = _renderer.UpdateContext.Lights?
                .OfType<DirectionalLight>()
                .FirstOrDefault();

            if (light == null)
                return;

            var depthTexture = _renderer.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (depthTexture == null)
                return;

            _passTarget.Configure(
                camera.ViewSize.Width,
                camera.ViewSize.Height,
                TextureFormat.GrayFloat16);

            UpdateUniforms(light, options);

            _passTarget.RenderTarget!.Begin(camera);

            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);
            _renderer.State.SetAlphaMode(AlphaMode.Opaque);
            _renderer.State.SetClearColor(Color.Transparent);


            _gl.Clear(ClearBufferMask.ColorBufferBit);

            GlState.Current!.SetActiveBuffer(_uniforms, CONTACT_SHADOW_BUF);

            _contactProgram.Use();

            GlState.Current!.LoadTexture(depthTexture, DEPTH_TEX_SLOT, true);

            DrawQuad();

            _passTarget.RenderTarget!.End(false);

            _renderer.RenderTarget!.Begin(camera);

            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);

            _applyProgram.Use();

            GlState.Current!.LoadTexture(_passTarget.ColorTexture!, MASK_TEX_SLOT, true);
            _applyProgram.SetUniform("uApplyStrength", 1f);

            _renderer.State.SetAlphaMode(AlphaMode.Blend);

            DrawQuad();
        }

        protected void UpdateUniforms(DirectionalLight light, ContactShadowOptions options)
        {
            var lightDir = Vector3.Normalize(-light.Direction);

            var data = new ContactShadowUniforms
            {
                LightDirWorld = new Vector4(lightDir, 0.0f),
                MaxDistance = options.MaxDistance,
                Thickness = options.Thickness,
                Strength = options.Strength,
                StepCount = options.StepCount,
                DepthBias = options.DepthBias,
                FadeDistance = options.FadeDistance,
                ViewSize = _renderer.UpdateContext.PassCamera.ViewSize.ToVector2()
            };

            _uniforms.Update(data);
        }

        public override void Dispose()
        {
            _contactProgram.Dispose();
            _passTarget.Dispose();
            _uniforms.Dispose();
            _applyProgram.Dispose();

            base.Dispose();
        }


        public GlRenderPassTarget PassTarget => _passTarget;

        public ContactShadowOptions Options => _renderer.Options.ContactShadow;
    }
}