#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlRenderBuffer : GlObject, IGlRenderAttachment
    {
        static readonly Dictionary<uint, GlRenderBuffer> _attached = [];

        protected uint _width;
        protected uint _height;
        protected uint _sampleCount;
        protected InternalFormat _internalFormat;

        public GlRenderBuffer(GL gl)
            : base(gl)
        {
            Create();
            Target = RenderbufferTarget.Renderbuffer;
        }

        public GlRenderBuffer(GL gl, uint handle, RenderbufferTarget target = RenderbufferTarget.Renderbuffer)
            : base(gl)
        {
            Attach(handle, target);
        }

        public void Update(uint width, uint height, uint sampleCount, InternalFormat internalFormat)
        {
            if (_width == width && _height == height && _sampleCount == sampleCount && _internalFormat == internalFormat)
                return;

            Bind();

            if (sampleCount > 1)
                _gl.RenderbufferStorageMultisample(Target, sampleCount, internalFormat, width, height);
            else
                _gl.RenderbufferStorage(Target, internalFormat, width, height);

            _width = width;
            _height = height;
            _sampleCount = sampleCount;
            _internalFormat = internalFormat;

            Unbind();
        }

        protected void Create()
        {
            _handle = _gl.GenRenderbuffer();
            _attached[_handle] = this;
        }

        public void Attach(uint handle, RenderbufferTarget target = RenderbufferTarget.Renderbuffer)
        {
            _attached[handle] = this;

            _handle = handle;

            Target = target != 0 ? target : RenderbufferTarget.Renderbuffer;
        }

        public void Bind()
        {
            _gl.BindRenderbuffer(Target, _handle);
        }

        public void Unbind()
        {
            _gl.BindRenderbuffer(Target, 0);
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                _gl.DeleteRenderbuffer(_handle);
                _attached.Remove(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }

        public static GlRenderBuffer Attach(GL gl, uint handle, RenderbufferTarget target = RenderbufferTarget.Renderbuffer)
        {
            if (!_attached.TryGetValue(handle, out var texture))
                texture = new GlRenderBuffer(gl, handle, target);
            return texture;
        }

        public uint Width => _width;

        public uint Height => _height;

        public InternalFormat InternalFormat => _internalFormat;

        public RenderbufferTarget Target { get; set; }
    }
}
