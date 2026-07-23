#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
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

        public GlShader(GL gl, ShaderType type, string source)
            : this(gl)
        {
            Create(type, source);
        }

        public void Create(ShaderType type, string source)
        {
            _handle = _gl.CreateShader(type);

            ShaderSource = source;
            Type = type;

            Update();
        }

        public void Update()
        {
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

                var cache = _shaders.First(a => a.Value == this);
                _shaders.Remove(cache.Key);

                base.Dispose();
            }
        }

        public static GlShader GetOrCreate(GL gl, ShaderType type, string source, string? name = null)
        {
            var sourceHash = HashBuilder.Instance.Compute(source);

            if (!_shaders.TryGetValue(sourceHash, out var shader))
            {
                shader = new GlShader(gl, type, source);
                shader.SetLabel(name);
                _shaders[sourceHash] = shader;
            }
            else
                shader._refCount++;

            return shader;
        }

        public ShaderType Type { get; set; }

        public string ShaderSource { get; set; }
    }
}
