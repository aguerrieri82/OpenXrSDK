#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text;

namespace OpenXr.Engine.OpenGL
{
    public class GlProgram : GlObject, IUniformProvider
    {
        readonly Dictionary<string, int> _locations = [];
        readonly GlRenderOptions _options;
        readonly IList<string>? _extensions;

        public GlProgram(GL gl, string vSource, string fSource, GlRenderOptions options) : base(gl)
        {
            _options = options;

            Create(
                LoadShader(ShaderType.VertexShader, vSource),
                LoadShader(ShaderType.FragmentShader, fSource)
            );
        }

        protected virtual void Create(params uint[] shaders)
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

        public virtual void SetAmbient(AmbientLight light)
        {
            SetUniform("light.ambient", (Vector3)light.Color * light.Intensity);
        }

        public virtual void AddPointLight(PointLight point)
        {
            var wordPos = Vector3.Transform(point.Transform.Position, point.WorldMatrix);

            SetUniform("light.diffuse", (Vector3)point.Color * point.Intensity);
            SetUniform("light.position", wordPos);
            SetUniform("light.specular", (Vector3)point.Specular);
        }

        public virtual void SetCamera(Camera camera)
        {
            SetUniform("uView", camera.Transform.Matrix);

            SetUniform("uProjection", camera.Projection);

            SetUniform("viewPos", camera.Transform.Position, true);
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

        protected int Locate(string name, bool optional = false, bool isBlock = false)
        {
            if (!_locations.TryGetValue(name, out var result))
            {
                if (isBlock)
                    result = (int)_gl.GetUniformBlockIndex(_handle, name);
                else
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

        public void SetUniformBuffer<T>(string name, GlBuffer<T> buffer) where T : unmanaged
        {
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)Locate(name, false, true), buffer.Handle);
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

        protected string PatchShader(string source, ShaderType shaderType)
        {
            var builder = new StringBuilder();

            builder.Append("#version ")
               .Append(_options.ShaderVersion!)
               .Append("\n");

            if (shaderType == ShaderType.VertexShader)
            {
                if (_options.ShaderExtensions != null)
                {
                    foreach (var ext in _options.ShaderExtensions)
                        builder.Append($"#extension {ext} : require\n");
                }
            }

            var precision = _options.FloatPrecision switch
            {
                ShaderPrecision.Medium => "highp",
                ShaderPrecision.High => "mediump",
                ShaderPrecision.Low => "lowp",
                _ => throw new NotSupportedException()
            };

            builder.Append("precision ").Append(precision).Append(" float;\n");

            PatchShader(source, shaderType, builder);

            builder.Append("\n\n").Append(source);

            return builder.ToString();
        }

        protected virtual void PatchShader(string source, ShaderType shaderType, StringBuilder builder)
        {

        }

        uint LoadShader(ShaderType type, string source)
        {
            uint handle = _gl.CreateShader(type);

            source = PatchShader(source, type);

            _gl.ShaderSource(handle, source);

            _gl.CompileShader(handle);

            string infoLog = _gl.GetShaderInfoLog(handle);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");

            return handle;
        }

    }
}
