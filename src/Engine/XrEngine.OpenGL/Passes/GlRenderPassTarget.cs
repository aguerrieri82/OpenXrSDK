#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.OpenGL
{
    public enum TargetDepthMode
    {
        None,
        Create,
        Existing
    }

    public class GlRenderPassTarget : IDisposable
    {
        class ExtraTexture
        {
            public GlTexture? Texture;

            public FramebufferAttachment? Attachment;

            public TextureFormat Format;    

            public bool IsMutable;  
        }

        private bool _mustDisposeColor;
        private IGlRenderTarget? _renderTarget;
        private GlTexture? _colorTexture;
        private IGlRenderAttachment? _depthBuffer;
        private GL _gl;
        private List<ExtraTexture> _extras = [];
        private bool _isDirty;

        public GlRenderPassTarget(GL gL)
        {
            _gl = gL;
            DepthMode = TargetDepthMode.Create;
            BoundEye = -1;
            DepthFormat = TextureFormat.Depth24Float;
        }

        [MemberNotNull(nameof(RenderTarget))]
        public void Configure(Texture colorTexture)
        {
            Configure(colorTexture.ToGlTexture());  
        }

        [MemberNotNull(nameof(RenderTarget))]
        public void Configure(GlTexture colorTexture)
        {
            _colorTexture = colorTexture;
            Configure(colorTexture.Width, colorTexture.Height, GlUtils.GetTextureFormat(colorTexture.InternalFormat));
        }

        public GlTexture? GetExtra(int id)
        {
            return _extras[id].Texture; 
        }

        public int AddExtra(TextureFormat format, FramebufferAttachment? attachment, bool isMutable)
        {
            var extra = new ExtraTexture()
            {
                Format = format,
                Attachment = attachment,
                IsMutable = isMutable
            };

            _extras.Add(extra);

            _isDirty = true;

            return _extras.Count - 1;
        }

        [MemberNotNull(nameof(RenderTarget))]
        public void Configure(uint width, uint height, TextureFormat format)
        {
            bool updateTarget = BoundEye != -1;

            bool isColorChanged = false;

            var depth = IsMultiView ? 2u : 1u;

            if (_renderTarget == null)
            {
                if (IsMultiView && UseMultiViewTarget)
                    _renderTarget = new GlMultiViewRenderTarget(_gl);
                else
                    _renderTarget = new GlTextureRenderTarget(_gl);

                updateTarget = true;
            }

            if (_colorTexture == null || _colorTexture.Width != width || _colorTexture.Height != height)
            {
                if (_mustDisposeColor)
                    _colorTexture?.Dispose();

                _colorTexture = new GlTexture(_gl)
                {
                    MinFilter = _colorTexture?.MinFilter ?? TextureMinFilter.Linear,
                    MagFilter = _colorTexture?.MagFilter ?? TextureMagFilter.Linear,
                    WrapS = _colorTexture?.WrapS ?? TextureWrapMode.ClampToEdge,
                    WrapT = _colorTexture?.WrapT ?? TextureWrapMode.ClampToEdge,
                    BorderColor = _colorTexture?.BorderColor ?? Color.White,
                    MaxLevel = 0,
                    Target = depth == 2 ? TextureTarget.Texture2DArray : TextureTarget.Texture2D
                };

                _colorTexture.Update(depth, new TextureData
                {
                    Width = width,
                    Height = height,
                    Format = format
                });

                _mustDisposeColor = true;
    
                updateTarget = true;
                isColorChanged = true;
            }

            if (DepthMode == TargetDepthMode.Create && ( _depthBuffer == null || isColorChanged))
            {
                if (_renderTarget is GlMultiViewRenderTarget)
                {
                    _depthBuffer?.Dispose();

                    _depthBuffer = new GlTexture(_gl)
                    {
                        MinFilter = TextureMinFilter.Nearest,
                        MagFilter = TextureMagFilter.Nearest,
                        MaxLevel = 0,
                        Target = TextureTarget.Texture2DArray
                    };

                    ((GlTexture)_depthBuffer).Update(2, new TextureData
                    {
                        Width = width,
                        Height = height,
                        Format = DepthFormat
                    });
                }
                else
                {
                    _depthBuffer ??= new GlRenderBuffer(_gl);

                    ((GlRenderBuffer)_depthBuffer!).Update(
                         width,
                         height,
                         1,
                         GlUtils.GetInternalFormat(DepthFormat, TextureCompressionFormat.Uncompressed));
                }
            }

            if (DepthMode == TargetDepthMode.Existing)
                _depthBuffer = OpenGLRender.Current?.RenderTarget?.QueryTexture(FramebufferAttachment.DepthAttachment);      

            if (isColorChanged || _isDirty)
            {
                FrameBuffer!.Bind();

                foreach (var extra in _extras)
                {
                    if (!extra.IsMutable)
                    {
                        extra.Texture?.Dispose();
                        extra.Texture = null;
                    }

                    extra.Texture ??= new GlTexture(_gl)
                    {
                        MinFilter = TextureMinFilter.Linear,
                        MagFilter = TextureMagFilter.Linear,
                        MaxLevel = 0,
                        IsMutable = extra.IsMutable,
                        Target = TextureTarget.Texture2D
                    };

                    extra.Texture.Update(depth, new TextureData
                    {
                        Width = width,
                        Height = height,
                        Format = extra.Format
                    });

                    if (extra.Attachment != null)
                        FrameBuffer!.BindAttachment(extra.Texture, extra.Attachment.Value, true);
                }

                _isDirty = false;   
            }

            if (updateTarget)
            {
                if (_renderTarget is GlMultiViewRenderTarget mv)
                    mv.FrameBuffer.Configure(_colorTexture, (GlTexture)_depthBuffer!, 1);

                else if (_renderTarget is GlTextureRenderTarget tex)
                {
                    if (BoundEye != -1)
                        tex.FrameBuffer.Configure(_colorTexture, (uint)BoundEye, _depthBuffer!, 0, 1);
                    else
                        tex.FrameBuffer.Configure(_colorTexture, _depthBuffer, 1);
                }
            }

            FrameBuffer!.Check();
        }

        public void Dispose()
        {
            if (_mustDisposeColor)
                _colorTexture?.Dispose();

            if (DepthMode == TargetDepthMode.Create)
                _depthBuffer?.Dispose();

            foreach (var extra in _extras)
                extra.Texture?.Dispose();   

            _renderTarget?.Dispose();

            _depthBuffer = null;
            _colorTexture = null;
            _renderTarget = null;

            GC.SuppressFinalize(this);  
        }

        public GlTexture? ColorTexture => _colorTexture;

        public IGlRenderAttachment? DepthBuffer => _depthBuffer;

        public IGlFrameBuffer? FrameBuffer => ((IGlFrameBufferProvider?)_renderTarget)?.FrameBuffer;   

        public IGlRenderTarget? RenderTarget => _renderTarget;

        public int BoundEye { get; set; }   

        public bool IsMultiView { get; set; }

        public bool UseMultiViewTarget { get; set; }    

        public TargetDepthMode DepthMode { get; set; }

        public TextureFormat DepthFormat { get; set; }
    }
}
