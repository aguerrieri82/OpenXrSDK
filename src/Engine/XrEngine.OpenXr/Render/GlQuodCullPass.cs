#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;
using OpenXr.Framework;
using XrEngine.Objects;


namespace XrEngine.OpenGL
{
    public class GlQuodCullPass : GlBaseRenderPass
    {

        protected readonly GlRenderPassTarget _passTarget;
        protected readonly GlSimpleProgram _program;
        protected readonly uint _sampleCount;
        protected readonly IQuodTexture _quod;


        public GlQuodCullPass(OpenGLRender renderer, IQuodTexture quod, bool isMultiView, uint sampleCount)
            : base(renderer)
        {
            _sampleCount = sampleCount;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                DepthMode = TargetDepthMode.None,
                IsMultiView = isMultiView,
                UseMultiViewTarget = false
            };

            _program = new GlSimpleProgram(
                renderer.GL,
                "[XrEngine.Core]fullscreen.vert",
                "quad_depth_cull.frag",
                str => Embedded.GetString<GlQuodCullPass>(str));

            var options = XrApp.Current!.RenderOptions;

            _program.AddFeature("CAMERA_UNIFORMS");
        
            if (isMultiView)
                _program.AddFeature("TEXTURE_ARRAY");

            if (sampleCount > 1)
            {
                _program.AddFeature("MULTISAMPLE");
                _program.AddFeature($"DEPTH_SAMPLES {sampleCount}");
            }
            /*
            _program.AddExtension("GL_OVR_multiview2");
            _program.AddFeature("MULTI_VIEW");
            */
            _program.Build();
            _quod = quod;
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            if (!_quod.EnableDepthCull || _quod.ActiveTexture == null || _quod.DrawTexture == null)
                return;

            var camera = ctx.PassCamera!;

            if (XrApp.Current == null)
                return;

            var targetIndex = _passTarget.IsMultiView ? 0 : _quod.ActiveEye;

            var projTarget = camera.GetProp<IGlRenderTarget>(OpenGLRender.Props.RenderTarget[targetIndex]);

            var depthTexture = projTarget?.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (depthTexture == null)
                return;

            _gl.MemoryBarrier(MemoryBarrierMask.FramebufferBarrierBit);

            _passTarget.BoundEye = _quod.ActiveEye;
            _passTarget.Configure(_quod.ActiveTexture);

            _program.Use();
            _program.SetUniform("uQuadWorld", _quod.WorldMatrix);
            _program.SetUniform("uDepthBias", _quod.DepthBias);

            if (_passTarget.IsMultiView)
                _program.SetUniform("uViewIndex", _quod.ActiveEye);

            _program.SetUniform("uViewProj", camera.Eyes == null || camera.Eyes.Length == 0 ?
                                camera.ViewProjection :
                                camera.Eyes[_quod.ActiveEye].ViewProj);

            _passTarget.RenderTarget!.Begin(camera);

            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);
            _renderer.State.SetAlphaMode(AlphaMode.Opaque);
            _renderer.State.SetClearColor(Color.Transparent);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            GlState.Current!.LoadTexture(depthTexture, TextureSlots.ProjDepth, true);
            GlState.Current!.LoadTexture(_quod.DrawTexture!.ToGlTexture(), TextureSlots.Albedo, true);

            DrawQuad();

            _passTarget.RenderTarget!.End(false);
        }


        public override void Dispose()
        {
            _program.Dispose();
            _passTarget.Dispose();

            base.Dispose();
        }


    }
}