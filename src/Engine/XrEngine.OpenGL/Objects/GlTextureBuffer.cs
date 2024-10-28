#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Reflection.Emit;
#endif

namespace XrEngine.OpenGL
{
    public class GlTextureBuffer : GlObject
    {
        readonly GlBuffer<byte> _buffer;
        readonly GlTexture _texture;
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

            _buffer.Bind();
      
            if (_width != data.Width || _height != data.Height)
            {
                _buffer.Allocate(data.Data!.Size);

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

            var ptr = _buffer.Map(MapBufferAccessMask.WriteBit);

            var pData = data.Data!.MemoryLock();

            System.Buffer.MemoryCopy(pData, ptr, _buffer.Length, data.Data.Size); 

            _buffer.Unmap();

            Bind();
   
            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, data.Width, data.Height, pixelFormat, pixelType, null);

            Unbind();

            _buffer.Unbind();
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
        }

        public GlBuffer<byte> Buffer => _buffer;

        public GlTexture Texture => _texture;

        public uint Width => _width;

        public uint Height => _height;

        public long Version { get; set; }
    }
}
