#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public abstract class GlObject : IDisposable
    {
        static readonly HashSet<GlObject> _catalog = [];
        static IGlContextProvider? _contextProvider;

        protected uint _handle;
        protected GL _gl;
        protected string? _label;
        protected bool _isDisposed;
        protected readonly IGlContext _owner;

        protected GlObject(GL gl)
        {
            _gl = gl;

            _contextProvider ??= Context.Require<IGlContextProvider>();

            _owner = _contextProvider.Current!;

            EnableDebug = true;
#if DEBUG
            _catalog.Add(this);
#endif
        }

        public virtual void Dispose()

        {
#if DEBUG
            _catalog.Remove(this);
#endif
            if (_handle != 0)
            {
                ObjectBinder.Unbind(this);
                _handle = 0;
            }

            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        public void SetLabel(string? label)
        {
            if (string.IsNullOrEmpty(label) || !OpenGLRender.Current!.IsDebug)
                return;

            if (_handle == 0)
            {
                _label = label;
                return;
            }

            ObjectIdentifier idType;

            if (this is GlTexture)
                idType = ObjectIdentifier.Texture;

            else if (this is GlSampler)
                idType = ObjectIdentifier.Sampler;

            else if (this is GlBaseFrameBuffer)
                idType = ObjectIdentifier.Framebuffer;

            else if (this is IGlBuffer)
                idType = ObjectIdentifier.Buffer;

            else if (this is GlRenderBuffer)
                idType = ObjectIdentifier.Renderbuffer;

            else if (this is IGlQuery)
                idType = ObjectIdentifier.Query;

            else if (this is GlBaseProgram)
                idType = ObjectIdentifier.Program;

            else if (this is GlShader)
                idType = ObjectIdentifier.Shader;

            else if (this is IGlVertexArray)
                idType = ObjectIdentifier.VertexArray;
            else
                return;

            label = $"{label} ({_handle})";

            _gl.ObjectLabel(idType, _handle, (uint)label.Length, label);
            _gl.ClearError();

            _label = label;
        }

        public static GlObject? FindObject(uint handle)
        {
            return _catalog.FirstOrDefault(a => a.Handle == handle);
        }

        public static implicit operator uint(GlObject obj)
        {
            return obj._handle;
        }

        public string? Label => _label;

        public uint Handle => _handle;

        public GL GL => _gl;

        public bool EnableDebug { get; set; }

        public object? Source { get; set; }

        public IGlContext Owner => _owner;
    }
}
