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
            DepthFormat = TextureFormat.Depth24Stencil8;
        }

        protected TextureTarget GetTarget(uint arraySize, uint sampleCount)
        {

            if (arraySize == 1)
                return sampleCount <= 1 ? TextureTarget.Texture2D : TextureTarget.Texture2DMultisample;

            return sampleCount <= 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2DMultisampleArray;
        }

        protected GlTexture CreateDepth(GlTexture color, uint arraySize, uint sampleCount)
        {
            var depthTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                SampleCount = sampleCount,
                MaxLevel = 0,
                Target = GetTarget(arraySize, sampleCount)
            };
  
            depthTex.Allocate(color.Width, color.Height, arraySize, DepthFormat);

            return depthTex;
        }

        public IGlRenderTarget CreateRenderTarget(uint width, uint height, TextureFormat format, uint sampleCount)
        {
            var texSampleCount = _multiView ? 1 : sampleCount;

            var arraySize = _multiView ? 2u : 1;

            var colorTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMagFilter.Linear,
                SampleCount = texSampleCount,
                MaxLevel = 0,
                Target = GetTarget(arraySize, texSampleCount)
            };

            colorTex.Allocate(width, height, arraySize, format);

            return GetRenderTarget(colorTex.Handle, 0, sampleCount);
        }

        public IGlRenderTarget GetRenderTarget(uint colorTex, uint depthTex, uint sampleCount, bool createDepth = true)
        {
            var targetId = colorTex * 10000 + depthTex;

            if (!_targets.TryGetValue(targetId, out var target))
            {
                GlTexture? glDepth = null;

                var texSampleCount = _multiView ? 1 : sampleCount;

                var glColor = GlTexture.Attach(_gl, colorTex, texSampleCount);

                if (depthTex != 0)
                    glDepth = GlTexture.Attach(_gl, depthTex, texSampleCount);

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);

                    if (createDepth)
                        glDepth ??= CreateDepth(glColor, 2, texSampleCount);

                    multiView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);

                    var useRenderTarget = !OpenGLRender.Current!.Options.UseDepthPass &&
                                          !OpenGLRender.Current!.Options.ContactShadow.Use;

                    //TODO: change
                    useRenderTarget = false;

                    if (useRenderTarget)
                    {
                        IGlRenderAttachment? depthAttachment = glDepth;

                        if (depthAttachment == null && createDepth)
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
                        if (createDepth)
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
