#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using static XrEngine.Ktx2Reader;

namespace XrEngine.OpenGL
{
    public class GlRenderTargetPool : IDisposable
    {
        private readonly GL _gl;
        private readonly bool _multiView;
        private readonly Dictionary<ulong, IGlRenderTargetFB> _targets = [];

        private GlTexture? _intermediateColor;


        public GlRenderTargetPool(GL gl, bool multiView)
        {
            _gl = gl;
            _multiView = multiView;
            DepthFormat = TextureFormat.Depth24Stencil8;
            IntermediateFormat = TextureFormat.RgbaFloat16;
        }

        protected static TextureTarget GetTarget(uint arraySize, uint sampleCount)
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

        protected GlTexture CreateIntermediateColor(GlTexture color, uint sampleCount)
        {
            var colorTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                SampleCount = sampleCount,
                MaxLevel = 0,
                Target = GetTarget(color.Depth, sampleCount)
            };

            colorTex.Allocate(color.Width, color.Height, color.Depth, IntermediateFormat);

            return colorTex;
        }

        public IGlRenderTarget CreateRenderTarget(uint width, uint height, uint sampleCount)
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

            colorTex.Allocate(width, height, arraySize, ColorFormat);

            return GetRenderTarget(colorTex.Handle, 0, sampleCount, -1);
        }

        public IGlRenderTargetFB GetRenderTarget(
            uint colorTex, 
            uint depthTex, 
            uint sampleCount, 
            int eyeIndex = -1,
            bool createDepth = true, 
            bool createColor = false)
        {
            var targetId =
                 ((ulong)colorTex << 33) |
                 ((ulong)depthTex << 2) |
                  (uint)(eyeIndex + 1);

            if (!_targets.TryGetValue(targetId, out var target))
            {
                GlTexture? glDepth = null;
                GlTexture? glColor = null;
                GlTexture? renderColor = null;

                var texSampleCount = _multiView ? 1 : sampleCount;

                if (depthTex != 0)
                    glDepth = GlTexture.Attach(_gl, depthTex, texSampleCount);

                if (colorTex != 0)
                    glColor = GlTexture.Attach(_gl, colorTex, texSampleCount);

                else if (createColor)
                {
                    Debug.Assert(glDepth != null);

                    glColor = new GlTexture(_gl)
                    {
                        MinFilter = TextureMinFilter.Linear,
                        MagFilter = TextureMagFilter.Linear,
                        SampleCount = glDepth.SampleCount,
                        MaxLevel = 0,
                        Target = glDepth.Target
                    };

                    glColor.Allocate(glDepth.Width, glDepth.Height, glDepth.Depth, ColorFormat);
                }

                if (glColor != null)
                {
                    glColor.SetLabel((Name ?? "RT Pool") + " - Color");

                    renderColor = glColor;

                    if (UseIntermediateColor)
                    {
                        _intermediateColor ??= CreateIntermediateColor(glColor, texSampleCount);
                        _intermediateColor.SetLabel((Name ?? "RT Pool") + " - Intermediate");
                        renderColor = _intermediateColor;
                    }
                }

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);

                    if (createDepth && glDepth == null)
                    {
                        Debug.Assert(renderColor != null);
                        glDepth = CreateDepth(renderColor, 2, texSampleCount);
                    }
   
                    multiView.FrameBuffer.Configure(renderColor, glDepth, sampleCount);

                    if (UseIntermediateColor)
                    {
                        Debug.Assert(glColor != null);

                        multiView.FrameBuffer.Attach(glColor, FramebufferAttachment.ColorAttachment1, false);
                        multiView.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0);
                        multiView.FrameBuffer.Check();
                    }

                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);

                    var useRenderTarget = !OpenGLRender.Current!.Options.UseDepthPass &&
                                          !OpenGLRender.Current!.Options.ContactShadow.Use;


#warning TEMPORARY DISABLED
                    useRenderTarget = false;

                    IGlRenderAttachment? depthAttachment = glDepth;

                    if (depthAttachment == null && createDepth && glDepth == null)
                    {
                        Debug.Assert(renderColor != null);

                        if (useRenderTarget)
                        {
                            var renderBuf = new GlRenderBuffer(_gl);
                            var intFormat = DepthFormat.ToInternalFormat();
                            renderBuf.Update(renderColor.Width, renderColor.Height, texSampleCount, intFormat);
                            depthAttachment = renderBuf;
                        }
                        else
                        {
                            glDepth = CreateDepth(renderColor, 1, texSampleCount);
                            depthAttachment = glDepth;
                        }
                    }

                    if (eyeIndex != -1)
                        singleView.FrameBuffer.Configure(renderColor, (uint)eyeIndex, depthAttachment, (uint)eyeIndex, sampleCount);
                    else
                        singleView.FrameBuffer.Configure(renderColor, depthAttachment, sampleCount);

                    if (UseIntermediateColor)
                    {
                        Debug.Assert(glColor != null);

                        singleView.FrameBuffer.Attach(glColor, FramebufferAttachment.ColorAttachment1, false);
                        singleView.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0);
                        singleView.FrameBuffer.Check();
                    }

                    target = singleView;
                }

                glDepth?.SetLabel((Name ?? "RT Pool") + " - Depth");

                _targets[targetId] = target;
            }

            return target;
        }

        public void Clear()
        {
            foreach (var item in _targets)
                item.Value.Dispose();

            _targets.Clear();

            _intermediateColor?.Dispose();
            _intermediateColor = null;
        }

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        public bool UseIntermediateColor { get; set; }

        public TextureFormat ColorFormat { get; set; }

        public TextureFormat DepthFormat { get; set; }

        public TextureFormat IntermediateFormat { get; set; }

        public GlTexture? IntermediateColor => _intermediateColor;

        public string? Name { get; set; }
    }
}