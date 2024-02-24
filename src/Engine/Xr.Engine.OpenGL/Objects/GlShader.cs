#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif


namespace Xr.Engine.OpenGL
{
    public class GlShader : GlObject
    {
        public GlShader(GL gl)
            : base(gl)
        {
            Source = "";
        }

        public GlShader(GL gl, ShaderType type, string source)
            : this(gl)
        {
            Create(type, source);
        }

        public void Create(ShaderType type, string source)
        {
            _handle = _gl.CreateShader(type);

            Source = source;
            Type = type;

            Update();
        }

        public void Update()
        {
            _gl.ShaderSource(_handle, Source);

            _gl.CompileShader(_handle);

            string infoLog = _gl.GetShaderInfoLog(_handle);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception($"Error compiling shader of type {Type}, failed with error {infoLog}");

        }

        public override void Dispose()
        {
            _gl.DeleteShader(_handle);
            _handle = 0;
        }

        public ShaderType Type { get; set; }

        public string Source { get; set; }
    }
}
