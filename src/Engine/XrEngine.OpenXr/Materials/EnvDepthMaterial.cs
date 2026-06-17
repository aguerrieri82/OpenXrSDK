using XrEngine.Objects;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class EnvDepthMaterial : ShaderMaterial, IShadowMaterial
    {
        public static readonly Shader SHADER;
        private Texture2D? _lastTexture;
        private PerspectiveCamera _depthCamera;
        private long _lastFrameTime;

        static EnvDepthMaterial()
        {
            SHADER = new StandardVertexShader
            {
                VertexSourceName = "[XrEngine.OpenXr]env_depth_mesh.vert",
                FragmentSourceName = "shadow_only.frag",
         
                IsLit = false
            };
        }

        public EnvDepthMaterial()
            : base()
        {
            _shader = SHADER;
            
            UseDepth = false;
            WriteDepth = false;
            DoubleSided = false;
            Priority = -1;
            Alpha = AlphaMode.BlendMain;
            ReceiveShadows = true;
            ShadowColor = new Color(0, 0, 0, 0.7f);

            _depthCamera = new PerspectiveCamera();
        }


        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            var options = bld.Context.ShadowMapProvider!.Options;


            bld.ExecuteAction((ctx, up) =>
            {
                var envDepth = ctx.PassCamera!.Feature<IEnvDepthProvider>();

                if (envDepth == null)
                    return;

               // envDepth.Blur = false;

                _lastTexture = envDepth.Acquire(_depthCamera, out _lastFrameTime);

                if (_lastTexture == null)
                    return;

                up.LoadTexture(_lastTexture, TextureSlots.EnvDepth);

                up.SetUniform("uShadowColor", ShadowColor);
                up.SetUniform("uColor", new Color(1, 1, 1, 1));
                up.SetUniform("uEnvViewProjInv[0]", _depthCamera.Eyes![0].ViewProjInv);
                up.SetUniform("uEnvViewProjInv[1]", _depthCamera.Eyes![1].ViewProjInv);
                up.SetUniform("uViewIndex", ctx.PassCamera!.ActiveEye);
            });

            base.UpdateShaderModel(bld);
        }

        public PerspectiveCamera DepthCamera => _depthCamera;

        public Texture2D? LastTexture => _lastTexture;

        public long LastFrameTime => _lastFrameTime;

        public Color ShadowColor { get; set; }

        public bool ReceiveShadows { get; set; }
    }
}
