﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text.RegularExpressions;
using System.Text;
using XrMath;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace XrEngine.OpenGL
{
    public abstract partial class GlBaseProgram : GlObject, IUniformProvider, IFeatureList
    {

        protected readonly List<string> _features = [];
        protected readonly List<string> _extensions = [];
        protected readonly Func<string, string> _resolver;
        protected readonly Dictionary<string, object> _values = [];
        protected readonly Dictionary<string, int> _locations = [];
        protected readonly Dictionary<string, int> _boundBuffers = [];


        public GlBaseProgram(GL gl, Func<string, string> includeResolver) : base(gl)
        {
            _resolver = includeResolver;
        }

        public abstract void Build();

        protected virtual void Create(params uint[] shaders)
        {
            _handle = _gl.CreateProgram();
            GlDebug.Log($"CreateProgram {_handle}");

            foreach (var shader in shaders.Where(a => a != 0))
                _gl.AttachShader(_handle, shader);

            _gl.LinkProgram(_handle);

            if (_gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus) == 0)
            {
                var log = _gl.GetProgramInfoLog(_handle);
                throw new Exception(log);
            }

            foreach (var shader in shaders.Where(a => a != 0))
                _gl.DetachShader(_handle, shader);

        }

        public void Use()
        {
            GlState.Current!.SetActiveProgram(this);

            GlDebug.Log($"UseProgram {_handle}");
        }

        public void Unbind()
        {
            GlState.Current!.SetActiveProgram(0);

            GlDebug.Log($"UseProgram NULL");
        }

        protected IEnumerable<string> GetUniformNames()
        {

#if GLES
            return [];
#else
            _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out int count);

            uint i = 0;

            while (i < count)
            {
                var buf = new string('\0', 256);
                _gl.GetActiveUniformName(_handle, i, (uint)buf.Length, out var len, out buf);
                yield return buf;
                i++;
            }
#endif
        }

        public int LocateUniform(string name, bool optional = false, bool isBlock = false)
        {
            if (!_locations.TryGetValue(name, out var result))
            {
                if (isBlock)
                    result = (int)_gl.GetUniformBlockIndex(_handle, name);
                else
                    result = _gl.GetUniformLocation(_handle, name);

                if (result == -1 && !optional) //TODO uncomment
                {
                    Log.Warn(this, "Uniform {0} not found", name);
                    Debug.WriteLine($"--- WARN --- {name} NOT FOUND");
                    //throw new Exception($"{name} uniform not found on shader.");
                }

                _locations[name] = result;
            }
            return result;
        }

        public void SetLineSize(float size)
        {
            _gl.LineWidth(size);
        }

        protected bool IsChanged(string name, object value)
        {
            bool isChanged;

            if (!_values.TryGetValue(name, out var lastValue))
                isChanged = true;
            else
            {
                if (lastValue is Array lastArray)
                {
                    var curArray = (Array)value;

                    if (lastArray.Length != curArray.Length)
                        return true;

                    var elSize = Marshal.SizeOf(lastArray.GetType()!.GetElementType()!);
                    var b1 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(lastArray), lastArray.Length * elSize);
                    var b2 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(curArray), curArray.Length * elSize);

                    isChanged = !b1.SequenceEqual(b2);
                }
                else
                    isChanged = !Equals(value, lastValue);
            }

            if (isChanged)
                _values[name] = value;

            return isChanged;
        }

        public void LoadTexture(Texture value, int slot = 0)
        {
            var tex2d = value as Texture2D ?? throw new NotSupportedException();

            var glText = tex2d.ToGlTexture();

            bool isUpdate = tex2d.Version != glText.Version && tex2d.Width > 0 && tex2d.Height > 0;

            GlState.Current!.SetActiveTexture(glText, slot, isUpdate);

            if (isUpdate)
                glText.Update(tex2d, false);

        }

        public void SetUniform(string name, int value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public void SetUniform(string name, uint value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.UniformMatrix4(LocateUniform(name, optional), 1, false, (float*)&value);
        }


        public unsafe void SetUniform(string name, Matrix3x3 value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.UniformMatrix3(LocateUniform(name, optional), 1, false, (float*)&value);
        }


        public void SetUniform(string name, float value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public void SetUniform(string name, Vector2 value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform2(LocateUniform(name, optional), value.X, value.Y);
        }

        public void SetUniform(string name, Vector3 value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform3(LocateUniform(name, optional), value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Color value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform4(LocateUniform(name, optional), value.R, value.G, value.B, value.A);
        }


        public void SetUniform(string name, IBuffer buffer, int slot = 0, bool optional = false)
        {
            var glBuffer = (IGlBuffer)buffer;

            _gl.BindBufferBase(glBuffer.Target, (uint)slot, glBuffer.Handle);

            var index = LocateUniform(name, optional, true);

            if (index == -1)
                return;

            if (!_boundBuffers.TryGetValue(name, out var curSlot) || slot != curSlot)
            {
                _gl.UniformBlockBinding(_handle, (uint)index, (uint)slot);
                _boundBuffers[name] = slot;
            }
        }

        public unsafe void SetUniform(string name, Texture value, int slot = 0, bool optional = false)
        {
            LoadTexture(value, slot);

            SetUniform(name, slot, optional);
        }

        public void SetUniform(string name, float[] value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            var span = value.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public unsafe void SetUniform(string name, Vector2[] value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;

            fixed (Vector2* data = value)
                _gl.Uniform2(LocateUniform(name, optional), (uint)value.Length, (float*)data);

        }
        public void SetUniform(string name, int[] value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            var span = value.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public void SetUniform(string name, Vector2I value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform2(LocateUniform(name, optional), value.X, value.Y);
        }

        public void AddFeature(string name)
        {
            _features.Add(name);
        }

        public void AddExtension(string name)
        {
            _extensions.Add(name);
        }

        protected string PatchShader(string source, ShaderType shaderType)
        {
            var builder = new StringBuilder();

            builder.Append("#version ")
               .Append(OpenGLRender.Current!.Options.ShaderVersion!)
               .Append('\n');

            foreach (var ext in _extensions)
                builder.Append($"#extension {ext} : require\n");

            var precision = OpenGLRender.Current!.Options.FloatPrecision switch
            {
                ShaderPrecision.Medium => "mediump",
                ShaderPrecision.High => "highp",
                ShaderPrecision.Low => "lowp",
                _ => throw new NotSupportedException()
            };

            builder.Append("precision ").Append(precision).Append(" float;\n");

            foreach (var feature in _features)
                builder.Append("#define ").Append(feature).Append('\n');

            if (shaderType == ShaderType.VertexShader)
                builder.Append("#define V_SHADER\n");

            PatchShader(shaderType, builder);

            var incRe = IncludeRegex();

            while (true)
            {
                var match = incRe.Match(source);
                if (!match.Success)
                    break;

                var incName = match.Groups.Count == 3 && match.Groups[2].Length > 0 ?
                    match.Groups[2].Value :
                    match.Groups[1].Value;

                source = string.Concat(
                    source.AsSpan(0, match.Index),
                    _resolver(incName),
                    "\n",
                    source.AsSpan(match.Index + match.Length)
                );
            }

            builder.Append("\n\n").Append(source);

            return builder.ToString();
        }

        protected virtual void PatchShader(ShaderType shaderType, StringBuilder builder)
        {
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                _gl.DeleteProgram(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex("#include\\s(?:(?:\"([^\"]+)\")|(?:<([^>]+)>));?\\s+")]
        protected static partial Regex IncludeRegex();
    }
}
