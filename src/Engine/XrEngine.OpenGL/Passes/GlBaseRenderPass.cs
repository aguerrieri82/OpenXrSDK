#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
#endif

namespace XrEngine.OpenGL
{
    public abstract class GlBaseRenderPass : IGlRenderPass
    {
        static OverlayTextureEffect _overlayEffect = new();
        static readonly Dictionary<ShaderMaterial, GlProgramInstance> _instances = [];

        protected readonly OpenGLRender _renderer;
        protected bool _isInit;
        protected GL _gl;
        protected GlRenderPassFlags _flags;

        public GlBaseRenderPass(OpenGLRender renderer)
        {
            _gl = renderer.GL;
            _renderer = renderer;
            IsEnabled = true;
        }

        public void UseEffect(ShaderMaterial material)
        {
            UseProgram(GetProgramInstance(material), true);
        }

        public virtual void Configure(GlUpdateContext ctx)
        {
        }

        protected virtual void Initialize()
        {
        }

        protected virtual IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type != GlLayerType.CastShadow);
        }

        public virtual void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            if (!_isInit)
            {
                Initialize();
                _isInit = true;
            }

            if (!BeginRender(ctx))
                return;

            foreach (var layer in SelectLayers())
            {
                layer.Prepare(ctx);

                if (layer is GlLayer glLayer)
                    RenderLayer(glLayer);
            }

            EndRender(ctx);
        }

        protected virtual bool BeginRender(GlUpdateContext ctx)
        {
            return true;
        }

        protected virtual void EndRender(GlUpdateContext ctx)
        {
        }

        protected virtual IGlRenderTarget? GetRenderTarget()
        {
            return _renderer.RenderTarget;
        }

        protected GlProgramInstance GetProgramInstance(ShaderMaterial material)
        {
            if (!_instances.TryGetValue(material, out var instance))
            {
                var global = material.Shader!.GetGlResource(gl => new GlProgramGlobal(_gl, material.Shader!));
                instance = new GlProgramInstance(_renderer.GL, material, global, null);
                _instances[material] = instance;
            }
            return instance;
        }

        protected void UseProgram(GlProgramInstance instance, bool updateUniforms)
        {
            var ctx = _renderer.UpdateContext;

            ctx.Shader = instance.Material.Shader;
            ctx.Stage = UpdateShaderStage.Shader;

            instance.Global!.UpdateProgram(ctx, GetRenderTarget()?.ShaderHandler);

            ctx.Stage = UpdateShaderStage.Material;

            instance.UpdateProgram(ctx);

            var programChanged = ctx.ProgramInstanceId != instance.Program!.Handle;

            ctx.ProgramInstanceId = instance.Program!.Handle;

            instance.Program.Use();

            if (programChanged)
                instance.Global.UpdateUniforms(ctx, instance.Program);

            _renderer.ConfigureCaps(instance.Material);

            if (updateUniforms)
            {
                instance.UpdateUniforms(ctx, false);
                instance.UpdateBuffers(ctx);
            }
        }

        protected void OverlayTexture(GlTexture texture, bool isMultiView)
        {
            OverlayTexture(texture.ToEngineTexture(), isMultiView);
        }

        protected void OverlayTexture(Texture texture, bool isMultiView)
        {
            _overlayEffect ??= new();
            _overlayEffect.Texture = texture;

            UseEffect(_overlayEffect);

            DrawQuad();
        }

        protected void DrawVirtual(uint vertices)
        {
            GlImageProc.DrawVirtual(_gl, vertices);
        }

        protected void DrawQuad()
        {
            GlImageProc.DrawQuad(_gl);
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public virtual void RenderLayer(GlLayer layer)
        {
            throw new NotSupportedException();
        }

        public GL Gl => _gl;

        public bool IsEnabled { get; set; }

        public GlRenderPassFlags Flags => _flags;
    }
}
