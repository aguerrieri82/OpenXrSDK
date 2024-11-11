#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Security.Cryptography;

#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseRenderPass : IGlRenderPass
    {
        static GlSimpleProgram? _drawQuad;
        static uint _emptyVertexArray;    

        protected readonly OpenGLRender _renderer;
        protected bool _isInit;
        protected GL _gl;

        public GlBaseRenderPass(OpenGLRender renderer)
        {
            _gl = renderer.GL;
            _renderer = renderer;
            IsEnabled = true;
        }

        public virtual void Configure(RenderContext ctx)
        {
        }

        protected virtual void Initialize()
        {
        }

        protected virtual IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type != GlLayerType.CastShadow);
        }

        public virtual void Render(RenderContext ctx)
        {
            if (!IsEnabled)
                return;

            if (!_isInit)
            {
                Initialize();
                _isInit = true;
            }

            if (!BeginRender(ctx.Camera!))
                return;

            foreach (var layer in SelectLayers())
            {
                layer.Prepare(ctx);
                RenderLayer(layer);
            }


            EndRender();
        }

        protected virtual bool BeginRender(Camera camera)
        {
            return true;
        }

        protected virtual void EndRender()
        {

        }

        protected virtual IGlRenderTarget? GetRenderTarget()
        {
            return _renderer.RenderTarget;
        }

        protected GlProgramInstance CreateProgram(ShaderMaterial material)
        {
            var global = material.Shader!.GetGlResource(gl => new GlProgramGlobal(_gl, material.Shader!));
            return new GlProgramInstance(_gl, material, global, null);
        }

        protected void UseProgram(GlProgramInstance instance, bool updateUniforms)
        {
            var updateContext = _renderer.UpdateContext;

            updateContext.Shader = instance.Material.Shader;

            instance.Global!.UpdateProgram(updateContext, GetRenderTarget() as IShaderHandler);

            instance.UpdateProgram(updateContext);

            bool programChanged = updateContext.ProgramInstanceId != instance.Program!.Handle;

            updateContext.ProgramInstanceId = instance.Program!.Handle;

            instance.Program.Use();

            if (programChanged)
                instance.Global.UpdateUniforms(updateContext, instance.Program);

            _renderer.ConfigureCaps(instance.Material);

            if (updateUniforms)
            {
                instance.UpdateUniforms(updateContext, false);
                instance.UpdateBuffers(updateContext);
            }

        }


        protected void ProcessImage(GlTexture src, GlTexture dst)
        {
            _gl.BindImageTexture(0, src, 0, false, 0, GLEnum.ReadOnly, src.InternalFormat);
            _gl.BindImageTexture(1, dst, 0, false, 0, GLEnum.WriteOnly, dst.InternalFormat);

            _gl.DispatchCompute((src.Width + 15) / 16, (src.Height + 15) / 16, 1);
            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        protected void OverlayTexture(GlTexture texture)
        {
            OverlayTexture(texture.ToEngineTexture());   
        }

        protected void OverlayTexture(Texture texture)
        {
            if (_drawQuad == null)
            {
                _drawQuad = new GlSimpleProgram(_gl, "fullscreen.vert", "texture_full.frag", str => Embedded.GetString<Material>(str));
                _drawQuad.Build();
            }

            if (_emptyVertexArray == 0)
                _emptyVertexArray = _gl.GenVertexArray();

            _drawQuad.Use();
            _drawQuad.LoadTexture(texture, 0);

            _renderer.State.SetUseDepth(false);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetAlphaMode(AlphaMode.Blend);

            _gl.BindVertexArray(_emptyVertexArray);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }


        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public abstract void RenderLayer(GlLayer layer);


        public bool IsEnabled { get; set; }
    }
}
