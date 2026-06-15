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
        private readonly bool _isGlEs;
        public GlFrameBufferPool(GL gl, bool multiView)
        {
            _gl = gl;
            _multiView = multiView;


#if GLES
            _isGlEs = true;
#endif

            DepthFormat = TextureFormat.Depth24Stencil8;
            //DepthFormat = TextureFormat.Depth16;
        }

        protected GlTexture CreateDepth(GlTexture color, uint arraySize, uint sampleCount)
        {
            var depthTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                SampleCount = sampleCount,
                MaxLevel = 0
            };

            if (arraySize == 1)
            {
                depthTex.Target = sampleCount <= 1 ? TextureTarget.Texture2D : TextureTarget.Texture2DMultisample;
                depthTex.Update(color.Width, color.Height, 1, DepthFormat);
            }
            else
            {
                depthTex.Target = sampleCount <= 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2DMultisampleArray;
                depthTex.Update(color.Width, color.Height, arraySize, DepthFormat);
            }

            return depthTex;
        }

        public IGlRenderTarget GetRenderTarget(uint colorTex, uint depthTex, uint sampleCount)
        {
            var targetId = colorTex * 10000 + depthTex;

            if (!_targets.TryGetValue(colorTex * 10000 + depthTex, out var target))
            {
                GlTexture? glDepth = null;

                var texSampleCount = _isGlEs && _multiView ? 1 : sampleCount;

                var glColor = GlTexture.Attach(_gl, colorTex, texSampleCount);

                if (depthTex != 0)
                    glDepth = GlTexture.Attach(_gl, depthTex, texSampleCount);

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);
                    
                    glDepth ??= CreateDepth(glColor, 2, texSampleCount);

                    multiView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);

                    var useRenderTarger = !OpenGLRender.Current!.Options.UseDepthPass &&
                                          !OpenGLRender.Current!.Options.ContactShadow.Use;

                    //TODO: change
                    useRenderTarger = false;

                    if (useRenderTarger)
                    {
                        IGlRenderAttachment? depthAttachment = glDepth;

                        if (depthAttachment == null)
                        {
                            var renderBuf = new GlRenderBuffer(_gl);
                            var intFormat = GlUtils.GetInternalFormat(DepthFormat, TextureCompressionFormat.Uncompressed);
                            renderBuf.Update(glColor.Width, glColor.Height, texSampleCount, intFormat);
                            depthAttachment = renderBuf;
                        }

                        singleView.FrameBuffer.Configure(glColor, depthAttachment, sampleCount);
                    }
                    else
                    {
                        glDepth ??= CreateDepth(glColor, 1, texSampleCount);

                        singleView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    }

                    target = singleView;
                }

                _targets[targetId] = target;
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

        public TextureFormat DepthFormat { get; set; }
    }
}
