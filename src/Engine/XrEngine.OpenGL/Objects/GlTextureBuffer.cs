#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlTextureBuffer : GlObject
    {
        readonly GlBuffer<byte> _buffer;
        readonly GlTexture _texture;
        int _alignment;
        uint _width;
        uint _height;
        private InternalFormat _format;

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
                Target = TextureTarget.Texture2D
            };

            _texture.Update();

            _handle = _texture.Handle;
        }



        public unsafe void Update(TextureData data)
        {
            GlUtils.GetPixelFormat(data.Format, out var pixelFormat, out var pixelType);

            _buffer.BeginUpdate();

            if (_width != data.Width || _height != data.Height)
            {
                _buffer.Allocate(data.Data!.Size);

                _alignment = GlUtils.CalculateUnpackAlignment(data.Width, GlUtils.GetPixelSizeBit(data.Format) / 8);

                _width = data.Width;
                _height = data.Height;
                _format = GlUtils.GetInternalFormat(data.Format, TextureCompressionFormat.Uncompressed);

                _texture.Bind();

                _gl.TexStorage2D(_texture.Target,
                       1,
                       (SizedInternalFormat)_format,
                       _width,
                       _height);
            }

            var pDst = _buffer.Map(MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

            using var pSrc = data.Data!.MemoryLock();

            EngineNativeLib.CopyMemory(pSrc, (nint)pDst, data.Data.Size);

            //Unsafe.CopyBlockUnaligned(pDst, pSrc, _buffer.Length);

            //System.Buffer.MemoryCopy(pSrc, pDst, _buffer.Length, data.Data.Size); 

            _buffer.Unmap();

            Bind();

            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, _alignment);

            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, data.Width, data.Height, pixelFormat, pixelType, null);

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
