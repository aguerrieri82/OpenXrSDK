#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGLES
{
    public class GlProgram : GlObject, IUniformProvider
    {
        public GlProgram(GL gl, string vSource, string fSource) : base(gl)
        {
            Create(
                LoadShader(ShaderType.VertexShader, vSource),
                LoadShader(ShaderType.FragmentShader, fSource)
            );
        }

        protected void Create(params uint[] shaders)
        {
            _handle = _gl.CreateProgram();

            foreach (var shader in shaders)
                _gl.AttachShader(_handle, shader);

            _gl.LinkProgram(_handle);

            if (_gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus) == 0)
            {
                var log = _gl.GetProgramInfoLog(_handle);
                throw new Exception(log);
            }

            foreach (var shader in shaders)
            {
                _gl.DetachShader(_handle, shader);
                _gl.DeleteShader(shader);
            }
        }


        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public void Unbind()
        {
            _gl.UseProgram(0);
        }

        public void SetUniform(string name, int value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
                throw new Exception($"{name} uniform not found on shader.");

            _gl.Uniform1(location, value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
                throw new Exception($"{name} uniform not found on shader.");

            _gl.UniformMatrix4(location, 1, false, (float*)&value);
        }

        public void SetUniform(string name, float value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
                throw new Exception($"{name} uniform not found on shader.");

            _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, Vector3 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        public override void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }

        private uint LoadShader(ShaderType type, string source)
        {
            uint handle = _gl.CreateShader(type);
            _gl.ShaderSource(handle, source);
            _gl.CompileShader(handle);
            
            string infoLog = _gl.GetShaderInfoLog(handle);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            
            return handle;
        }

        private uint LoadShaderFromPath(ShaderType type, string path)
        {
            var src = File.ReadAllText(path);

            return LoadShader(type, src);
        }
    }
}
