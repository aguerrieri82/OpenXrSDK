#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Net.Mail;

#endif

using XrMath;
using static System.Net.Mime.MediaTypeNames;


namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseRenderPass
    {
        private uint _fooVa;
        //private GlTexture? _depthTex;
        //private GlProgramInstance? _copyDepth;
        private GlProgramInstance? _outline;
        private GlProgramInstance? _clear;


        public GlOutlinePass(OpenGLRender renderer)
            : base(renderer)    
        {
        }

        protected override bool BeginRender()
        {
            _renderer.RenderTarget!.Begin();
            return base.BeginRender();
        }

        protected override void EndRender()
        {
            //_renderer.RenderTarget!.End(false);
        }

        protected override void Initialize()
        {
            var options = _renderer.Options.Outline;

            _outline = CreateProgram(new OutlineEffect()
            {
                Color = options.Color,
                Size = options.Size,    
            });
            
            _clear = CreateProgram(new DepthClearEffect()
            {
                StencilFunction = StencilFunction.NotEqual,
                CompareStencil = options.ActiveOutlineStencil
            });

            /*
            _copyDepth = CreateProgram(new DepthCopyEffect()
            {
            });
            */
            _fooVa = _renderer.GL.GenVertexArray();

            base.Initialize();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main).Take(1);
        }

        protected override void RenderLayer(GlLayer layer)
        {
            /*
            var fb = ((_renderer.RenderTarget) as IGlFrameBufferProvider)!.FrameBuffer;

            if (_depthTex == null)
            {
                _depthTex = new GlTexture(_renderer.GL);
                _depthTex.Target = TextureTarget.Texture2DMultisample;
                _depthTex.SampleCount = 2;
                _depthTex.Update(fb.Depth!.Width, fb.Depth.Height, 1, TextureFormat.RFloat32);
            }

            fb.SetDrawBuffers(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);

            _renderer.GL.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment1,
                _depthTex.Target,
                _depthTex, 0);

            fb.Check();

            UseProgram(_copyDepth!, true);

            _renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            */

            _renderer.GL.BindVertexArray(_fooVa);

            UseProgram(_clear!, true);

            _renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _renderer.RenderTarget?.CommitDepth();
            
            _renderer.UpdateContext.DepthMap = _renderer.GetDepth();

            UseProgram(_outline!, true);

            _renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _renderer.GL.BindVertexArray(0);

            _renderer.UpdateContext.DepthMap = null;
        }


    }
}
