#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public class GlShader : GlObject
    {
        public GlShader(GL gl)
            : base(gl)  
        {
        }

        public GlShader(GL gl, ShaderType type, string source)
            : this(gl)
        {
            Create(type, source);
        }

        public void Create(ShaderType type, string source)
        {
            _handle = _gl.CreateShader(type);

            _gl.ShaderSource(_handle, source);

            _gl.CompileShader(_handle);

            string infoLog = _gl.GetShaderInfoLog(_handle);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");

        }

        public override void Dispose()
        {
            _gl.DeleteShader(_handle);
            _handle = 0;
        }
    }
}
