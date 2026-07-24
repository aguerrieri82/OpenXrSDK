#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Collections.Concurrent;

#endif

using System.Security.Cryptography;
using System.Text;
using XrEngine.Helpers;

namespace XrEngine.OpenGL
{
    public class GlShader : GlObject
    {
        static readonly Dictionary<ulong, GlShader> _shaders = [];

        protected int _refCount;

        public GlShader(GL gl)
            : base(gl)
        {
            ShaderSource = string.Empty;
            _refCount++;
        }

        public GlShader(GL gl, ShaderType type, string source, string? name)
            : this(gl)
        {
            Create(type, source, name);
        }

        public void Create(ShaderType type, string source, string? name)
        {
            _handle = _gl.CreateShader(type);

            SetLabel(name);

            ShaderSource = source;
            Type = type;

            Update();
        }

        public void Update()
        {
            Log.Info(this, "Building shader '{0}'", _label);

            _gl.ShaderSource(_handle, ShaderSource);

            _gl.CompileShader(_handle);

            var infoLog = _gl.GetShaderInfoLog(_handle);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception($"Error compiling shader of type {Type}, failed with error {infoLog}");
        }

        public override void Dispose()
        {
            _refCount--;

            if (_refCount <= 0 && _handle != 0)
            {
                _gl.DeleteShader(_handle);

                lock (_shaders)
                {
                    var cache = _shaders.First(a => a.Value == this);

                    _shaders.Remove(cache.Key);
                }

                base.Dispose();
            }
        }

        public static GlShader GetOrCreate(GL gl, ShaderType type, string source, string? name = null)
        {
            var sourceHash = HashBuilder.Instance.Compute(source);

            lock (_shaders)
            {
                if (!_shaders.TryGetValue(sourceHash, out var shader))
                {
                    shader = new GlShader(gl, type, source, name);

                    _shaders[sourceHash] = shader;
                }
                else
                    shader._refCount++;

                return shader;
            }
        }

        public ShaderType Type { get; set; }

        public string ShaderSource { get; set; }
    }
}
