#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Reflection.Emit;
#endif

namespace XrEngine.OpenGL
{
    public abstract class GlObject : IDisposable
    {
        protected uint _handle;
        protected GL _gl;
        protected string? _label;

        protected GlObject(GL gl)
        {
            _gl = gl;
            EnableDebug = true;
        }

        public virtual void Dispose()
        {
            if (_handle != 0)
            {
                ObjectBinder.Unbind(this);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }

        public void SetLabel(string? label)
        {
            if (string.IsNullOrEmpty(label) || _handle == 0 || !OpenGLRender.Current!.IsDebug)
                return;

            if (_gl.IsTexture(_handle))
            {
                _gl.ObjectLabel(ObjectIdentifier.Texture, _handle, (uint)label.Length, label);
                _gl.CheckError();
            }

            _label = label;
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
    }
}
