#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlFrameBufferPool : IDisposable
    {
        private readonly GL _gl;
        private readonly bool _multiView;
        private readonly Dictionary<uint, IGlRenderTarget> _targets = [];

        public GlFrameBufferPool(GL gl, bool multiView)
        {
            _gl = gl;
            _multiView = multiView;
            DepthFormat = TextureFormat.Depth32Stencil8;
        }

        protected GlTexture CreateDepth(GlTexture color, uint arraySize)
        {
            var depthTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                MaxLevel = 0
            };

            if (arraySize == 1)
            {
                depthTex.Target = TextureTarget.Texture2D;
                depthTex.Update(color.Width, color.Height, 1, DepthFormat);
            }
            else
            {
                depthTex.Target = TextureTarget.Texture2DArray;
                depthTex.Update(color.Width, color.Height, arraySize, DepthFormat);
            }

            return depthTex;
        }

        public IGlRenderTarget GetRenderTarget(uint colorTex, uint sampleCount)
        {
            if (!_targets.TryGetValue(colorTex, out var target))
            {
                var glColor = GlTexture.Attach(_gl, colorTex);

                GlTexture glDepth;

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);
                    glDepth = CreateDepth(glColor, 2);
                    multiView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);
                    if (sampleCount > 0)
                    {
                        var renderBuf = new GlRenderBuffer(_gl);
                        var intFormat = GlTexture.GetInternalFormat(DepthFormat, TextureCompressionFormat.Uncompressed);

                        renderBuf.Update(glColor.Width, glColor.Height, sampleCount, intFormat);
                        singleView.FrameBuffer.Configure(glColor, renderBuf, sampleCount);
                    }
                    else
                    {
                        glDepth = CreateDepth(glColor, 1);
                        singleView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    }

                    target = singleView;
                }

                _targets[colorTex] = target;
            }

            return target;
        }

        public void Clear()
        {
            foreach (var item in _targets)
                item.Value.Dispose();   
            _targets.Clear();   
        }

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        public TextureFormat DepthFormat {  get; set; } 
    }
}
