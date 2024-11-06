#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlBloomPass : IGlRenderPass, IBloomProvider
    {
        private readonly OpenGLRender _renderer;
        private readonly GlComputeProgram _programH;
        private readonly GlComputeProgram _programV;

        public GlBloomPass(OpenGLRender renderer)
        {
            _renderer = renderer;

            Scale = 1;

            _programH = new GlComputeProgram(renderer.GL, "Image/bloom.glsl", str => Embedded.GetString<Material>(str));
            _programH.AddFeature("MODE 0");
            _programH.Build();

            _programV = new GlComputeProgram(renderer.GL, "Image/bloom.glsl", str => Embedded.GetString<Material>(str));
            _programV.AddFeature("MODE 1");
            _programV.Build();
        }

        public bool IsEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Configure()
        {
        }

        public void Dispose()
        {
            _programH.Dispose();
            _programV.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Render(RenderContext ctx)
        {
            var curProgram = _renderer.State.ActiveProgram;
            var color = ((GlTextureFrameBuffer)((IGlFrameBufferProvider)_renderer.RenderTarget!).FrameBuffer).Color!;

            _programH.Use();
            _programH.SetUniform("uScale", Scale);

            _renderer.GL.BindImageTexture(0, color, 0, false, 0, GLEnum.ReadWrite, color.InternalFormat);
            _renderer.GL.DispatchCompute((color.Width + 15) / 16, (color.Height + 15) / 16, color.Depth);
            _renderer.GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _programV.Use();
            _programV.SetUniform("uScale", Scale);

            _renderer.GL.BindImageTexture(0, color, 0, false, 0, GLEnum.ReadWrite, color.InternalFormat);
            _renderer.GL.DispatchCompute((color.Width + 15) / 16, (color.Height + 15) / 16, color.Depth);
            _renderer.GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _renderer.State.SetActiveProgram(curProgram ?? 0);

        }

        public float Scale { get; set; }
    }
}
