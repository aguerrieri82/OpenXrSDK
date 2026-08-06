#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public class GlTextureBuffer : GlObject
    {
        readonly GlBuffer<byte> _buffer;
        readonly GlTexture _texture;
        int _alignment;
        uint _width;
        uint _height;
        private TextureFormat _format;

        public GlTextureBuffer(GL gl)
            : base(gl)
        {
            _buffer = new GlBuffer<byte>(gl, BufferTargetARB.PixelUnpackBuffer);

            _texture = new GlTexture(_gl)
            {
                MaxLevel = 0,
                WrapS = TextureWrapMode.ClampToEdge,
                WrapT = TextureWrapMode.ClampToEdge,
                MagFilter = TextureMagFilter.Linear,
                MinFilter = TextureMinFilter.Linear,
                Target = TextureTarget.Texture2D,
                IsMutable = false
            };

            _texture.UpdateSampler();

            _handle = _texture.Handle;
        }

        public unsafe void Update(TextureData texData)
        {
            Debug.Assert(texData.Content != null);

            GlUtils.GetPixelFormat(texData.Format, out var pixelFormat, out var pixelType);

            _buffer.BeginUpdate();

            if (_width != texData.Width || _height != texData.Height)
            {
                _buffer.Allocate(texData.Content!.Size);

                _alignment = GlUtils.CalculateUnpackAlignment(texData.Width, texData.Format.GetPixelSizeBit() / 8);

                _width = texData.Width;
                _height = texData.Height;
                _format = texData.Format;

                _texture.Allocate(_width, _height, 1, _format);
            }

            _buffer.Update(texData.Content);

            Bind();

            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, _alignment);

            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texData.Width, texData.Height, pixelFormat, pixelType, null);

            Unbind();

            _buffer.EndUpdate();
        }

        public void Bind()
        {
            _texture.Bind();
        }

        public void Unbind()
        {
            _texture.Unbind();
        }

        public override void Dispose()
        {
            _texture.Dispose();
            _buffer.Dispose();
            base.Dispose();
        }

        public GlBuffer<byte> Buffer => _buffer;

        public GlTexture Texture => _texture;

        public uint Width => _width;

        public uint Height => _height;

        public long Version { get; set; }
    }
}
