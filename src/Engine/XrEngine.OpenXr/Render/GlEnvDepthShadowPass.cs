#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class GlEnvDepthShadowPass : GlBaseRenderPass
    {
        protected Camera _depthCamera;

        protected readonly GlSimpleProgram _program;

        public GlEnvDepthShadowPass(OpenGLRender renderer) : base(renderer)
        {
            UseGrid = true;
            GridSize = new Size2I(50, 50);

            _depthCamera = new PerspectiveCamera();

            _program = new GlSimpleProgram(
                renderer.GL,
                UseGrid ? "env_depth_shadow.vert" : "[XrEngine.Core]fullscreen.vert",
                UseGrid ? "env_depth_shadow_grid.frag" : "env_depth_shadow.frag",
                str => Embedded.GetString<GlQuodCullPass>(str));

            _program.AddFeature("USE_SHADOW_MAP");
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            var shadowProvider = ctx.ShadowMapProvider;

            if (shadowProvider == null || shadowProvider.Options.Mode == ShadowMapMode.None)
                return;

            if (shadowProvider.ShadowMap == null)
                return;

            var isMultiView = _renderer.RenderTarget is GlMultiViewRenderTarget;

            var camera = ctx.MainCamera!;

            var envDepth = camera.Feature<IEnvDepthProvider>();

            if (envDepth == null)
                return;

            var envDepthTex = envDepth.Acquire(_depthCamera);

            if (envDepthTex == null)
                return;

            var projDepthTex = _renderer.RenderTarget?.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (projDepthTex == null)
                return;

            var shOptions = shadowProvider.Options;

            if (!_program.IsBuilt)
            {
                if (isMultiView)
                {
                    _program.AddExtension("GL_OVR_multiview2");
                    _program.AddFeature("MULTI_VIEW");
                }
                else
                    _program.AddFeature("CAMERA_UNIFORMS");

                if (projDepthTex.SampleCount > 1)
                {
                    _program.AddFeature("MULTISAMPLE");
                    _program.AddFeature($"DEPTH_SAMPLES {projDepthTex.SampleCount}");
                }

                _program.AddFeature("SHADOW_MAP_MODE " + (int)shOptions.Mode);
                _program.AddFeature("SHADOW_BIAS " + (int)shOptions.BiasMode);

                if (shOptions.UseShadowSampler)
                    _program.AddFeature("USE_SHADOW_SAMPLER");

                _program.Build();

            }

            _renderer.State.SetUseDepth(false);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);
            _renderer.State.SetAlphaMode(AlphaMode.Blend);
            _renderer.State.Commit();

            _program.Use();

            _renderer.State.LoadTexture(projDepthTex, TextureSlots.ProjDepth);
            _renderer.State.LoadTexture(envDepthTex.ToGlTexture(), TextureSlots.EnvDepth); ;
            _renderer.State.LoadTexture(shadowProvider.ShadowMap!.ToGlTexture(), TextureSlots.ShadowMap, false);

            if (shOptions.BiasMode == ShadowMapBiasMode.Value)
                _program.SetUniform("uShadowBias", shOptions!.Bias);

            if (shOptions.Mode == ShadowMapMode.VSM)
                _program.SetUniform("uLightBleed", shOptions!.LightBleed);

            _program.SetUniform("uShadowColor", ShadowColor);
            _program.SetUniform("uEnvDepthBias", DepthBias);
            _program.SetUniform("uLightMatrix", shadowProvider.LightCamera!.ViewProjection);
            _program.SetUniform("uEnvViewProjInv[0]", _depthCamera.Eyes![0].ViewProjInv);
            _program.SetUniform("uEnvViewProjInv[1]", _depthCamera.Eyes![1].ViewProjInv);

            if (!isMultiView)
            {
                _program.SetUniform("uViewIndex", camera.ActiveEye);
                _program.SetUniform("uViewProj", camera.Eyes == null || camera.Eyes.Length == 0 ?
                    camera.ViewProjection :
                    camera.Eyes[camera.ActiveEye].ViewProj);
            }

            if (UseGrid)
            {
                var vertexCount = (GridSize.Width - 1) * (GridSize.Height - 1) * 6;
                _program.SetUniform("uGridSize", new Vector2I((int)GridSize.Width, (int)GridSize.Height));
                _renderer.State.EnableFeature(EnableCap.CullFace, false);
                DrawVirtual(vertexCount);
            }
            else
                DrawQuad();
        }

        public bool UseGrid { get; set; }

        public Size2I GridSize { get; set; }

        public Color ShadowColor { get; set; }

        public float DepthBias { get; set; }
    }
}
