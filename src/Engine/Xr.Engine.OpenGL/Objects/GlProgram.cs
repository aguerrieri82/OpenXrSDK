#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Reflection;
using System.Text;
using System.Collections;


namespace OpenXr.Engine.OpenGL
{
    public abstract class GlProgram : GlObject, IUniformProvider
    {
        protected readonly Dictionary<string, int> _locations = [];
        protected readonly GlRenderOptions _options;

        public GlProgram(GL gl, GlRenderOptions options) : base(gl)
        {
            _options = options;

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

        protected int LocateUniform(string name, bool optional = false, bool isBlock = false)
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
            _gl.Uniform1(LocateUniform(name), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            _gl.UniformMatrix4(LocateUniform(name), 1, false, (float*)&value);
        }

        public void SetUniform(string name, float value)
        {
            _gl.Uniform1(LocateUniform(name), value);
        }

        public void SetUniform(string name, Vector3 value, bool optional = false)
        {
            _gl.Uniform3(LocateUniform(name, optional), value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Color value)
        {
            _gl.Uniform4(LocateUniform(name), value.R, value.G, value.B, value.A);
        }

        public void SetUniformBuffer<T>(string name, GlBuffer<T> buffer) where T : unmanaged
        {
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)LocateUniform(name, false, true), buffer.Handle);
        }

        public unsafe void SetUniform(string name, Texture2D value, int slot = 0)
        {
            var texture = value.GetResource(a => value.CreateGlTexture(_gl));

            texture.Bind();
            _gl.ActiveTexture(TextureUnit.Texture0 + slot);
            SetUniform(name, slot);
        }

        public void SetUniform(string name, float[] obj)
        {
            var span = obj.AsSpan();  
            _gl.Uniform1(LocateUniform(name), span);
        }

        public void SetUniform(string name, int[] obj)
        {
            var span = obj.AsSpan();
            _gl.Uniform1(LocateUniform(name), span);
        }

        public void SetUniformStruct(string name, object obj)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fullName = $"{name}.{field.Name}";
                SetUniformObject(fullName, field.GetValue(obj)!);
            }
        }

        public void SetUniformStructArray(string name, ICollection collection)
        {
            var i = 0;
            foreach (var item in collection)
            {
                SetUniformStruct($"{name}[{i}]", item);
                i++;
            }
        }

        public unsafe void SetUniformObject(string name, object obj) 
        {
            if (obj is Vector3 vec3)
                SetUniform(name, vec3);
            else if (obj is Matrix4x4 mat4)
                SetUniform(name, mat4);
            else if (obj is float flt)
                SetUniform(name, flt);
            else if (obj is int vInt)
                SetUniform(name, vInt);
            if (obj is float[] fArray)
                SetUniform(name, fArray);
            if (obj is int[] iArray)
                SetUniform(name, iArray);
            else
            {
                var type = obj.GetType();
                
                if (type.IsValueType && !type.IsEnum && !type.IsPrimitive)
                    SetUniformStruct(name, obj);

                else if (obj is ICollection coll)
                {
                    var gen = type.GetInterfaces()
                            .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(ICollection<>));
                    var elType = gen.GetGenericArguments()[0];
                    if (elType.IsValueType && !elType.IsEnum && !elType.IsPrimitive)
                        SetUniformStructArray(name, coll);
                }
                else
                    throw new NotSupportedException();
            }
        }

        public override void Dispose()
        {
            _gl.DeleteProgram(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

    }
}
