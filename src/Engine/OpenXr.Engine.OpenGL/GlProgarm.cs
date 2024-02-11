﻿#if GLES
using OpenXr.Engine.OpenGL;
using Silk.NET.OpenGLES;
using SkiaSharp;

#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;

namespace OpenXr.Engine.OpenGL
{
    public class GlProgram : GlObject, IUniformProvider
    {
        readonly Dictionary<string, int> _locations = [];

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
            GlDebug.Log($"CreateProgram {_handle}");

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
            GlDebug.Log($"UseProgram {_handle}");
        }

        public void Unbind()
        {
            _gl.UseProgram(0);
            GlDebug.Log($"UseProgram NULL");
        }

        protected int Locate(string name, bool optional = false)
        {
            if (!_locations.TryGetValue(name, out var result))
            {
                result = _gl.GetUniformLocation(_handle, name);
                if (result == -1 && !optional)
                    throw new Exception($"{name} uniform not found on shader.");
                _locations[name] = result;  
            }
            return result;
        }

        public void SetUniform(string name, int value)
        {
            _gl.Uniform1(Locate(name), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            _gl.UniformMatrix4(Locate(name), 1, false, (float*)&value);
        }

        public void SetUniform(string name, float value)
        {
            _gl.Uniform1(Locate(name), value);
        }

        public void SetUniform(string name, Vector3 value, bool optional = false)
        {
            _gl.Uniform3(Locate(name, optional), value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Color value)
        {
            _gl.Uniform4(Locate(name), value.R, value.G, value.B, value.A);
        }

        public unsafe void SetUniform(string name, Texture2D value, int slot = 0)
        {
            var texture = value.GetResource(a => value.CreateGlTexture(_gl));

            texture.Bind();
            _gl.ActiveTexture(TextureUnit.Texture0 + slot);
            SetUniform(name, slot);
        }

        public override void Dispose()
        {
            _gl.DeleteProgram(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
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