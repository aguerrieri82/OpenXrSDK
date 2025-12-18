#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text.RegularExpressions;
using System.Text;
using XrMath;
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
        protected readonly int[] _boundBuffers = new int[32];


        public GlBaseProgram(GL gl, Func<string, string> includeResolver) : base(gl)
        {
            _resolver = includeResolver;
        }

        public abstract void Build();

        protected virtual void Create(params uint[] shaders)
        {
            _handle = _gl.CreateProgram();
            GlDebug.Log($"CreateProgram {_handle}");

            foreach (uint shader in shaders.Where(a => a != 0))
                _gl.AttachShader(_handle, shader);

            _gl.LinkProgram(_handle);

            if (_gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus) == 0)
            {
                string log = _gl.GetProgramInfoLog(_handle);
                throw new Exception(log);
            }

            foreach (uint shader in shaders.Where(a => a != 0))
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
                string? buf = new string('\0', 256);
                _gl.GetActiveUniformName(_handle, i, (uint)buf.Length, out uint len, out buf);
                yield return buf;
                i++;
            }
#endif
        }

        public int LocateUniform(string name, bool optional = false, bool isBlock = false)
        {
            if (!_locations.TryGetValue(name, out int result))
            {
                if (isBlock)
                    result = (int)_gl.GetUniformBlockIndex(_handle, name);
                else
                    result = _gl.GetUniformLocation(_handle, name);

                if (result == -1 && !optional) //TODO uncomment
                {
                    Log.Warn(this, "Uniform {0} not found", name);
                    //Debug.WriteLine($"--- WARN --- {name} NOT FOUND");
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
            bool isChanged = false;

            if (!_values.TryGetValue(name, out object? lastValue))
                isChanged = true;

            if (lastValue is Array lastArray)
            {
                Array curArray = (Array)value;

                if (!isChanged)
                {
                    if (lastArray.Length != curArray.Length)
                        isChanged = true;
                    else
                    {
                        int elSize = Marshal.SizeOf(lastArray.GetType()!.GetElementType()!);
                        ReadOnlySpan<byte> b1 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(lastArray), lastArray.Length * elSize);
                        ReadOnlySpan<byte> b2 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(curArray), curArray.Length * elSize);

                        isChanged = !b1.SequenceEqual(b2);
                    }
                }

                if (isChanged)
                    _values[name] = curArray.Clone();
            }
            else
            {
                if (!isChanged)
                    isChanged = !Equals(value, lastValue);

                if (isChanged)
                    _values[name] = value;
            }
            return isChanged;
        }

        public void LoadTexture(Texture value, int slot = 0)
        {
            Texture2D tex2d = value as Texture2D ?? throw new NotSupportedException();

            if (tex2d.Type == TextureType.Buffer)
            {
                if (!ObjectBinder.TryGet(tex2d, out GlTextureBuffer? glTextBuf))
                {
                    glTextBuf = new GlTextureBuffer(_gl);
                    ObjectBinder.Bind(tex2d, glTextBuf);
                }

                bool isUpdate = tex2d.Version != glTextBuf.Version && tex2d.Data != null && tex2d.Data.Count > 0;

                if (isUpdate)
                    glTextBuf.Update(tex2d.Data![0]);

                GlState.Current!.LoadTexture(glTextBuf.Texture, slot);

                glTextBuf.Version = tex2d.Version;
            }
            else
            {
                if (!ObjectBinder.TryGet(tex2d, out GlTexture? glText))
                    glText = tex2d.ToGlTexture();

                bool isUpdate = tex2d.Version != glText.Version && tex2d.Width > 0 && tex2d.Height > 0;

#if GLES
                GlState.Current!.LoadTexture(glText, slot, false);
#else
                GlState.Current!.LoadTexture(glText, slot);
#endif

                if (isUpdate)
                    glText.Update(tex2d, false);
            }
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


        public unsafe void SetUniform(string name, Vector4 value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform4(LocateUniform(name, optional), value.X, value.Y, value.Z, value.W);
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

        public void LoadBuffer<T>(IBuffer<T> buffer, int slot = 0)
        {
            IGlBuffer glBuffer = (IGlBuffer)buffer;

            GlState.Current!.SetActiveBuffer(glBuffer, slot);
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
            Span<float> span = value.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public unsafe void SetUniform(string name, Vector2[] value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;

            fixed (Vector2* data = value)
                _gl.Uniform2(LocateUniform(name, optional), (uint)value.Length, (float*)data);

        }

        public unsafe void SetUniform(string name, Vector3[] value, bool optional = false)
        {
            if (value.Length < 5 && !IsChanged(name, value))
                return;

            fixed (Vector3* data = value)
                _gl.Uniform3(LocateUniform(name, optional), (uint)value.Length, (float*)data);

        }

        public unsafe void SetUniform(string name, Vector4[] value, bool optional = false)
        {
            fixed (Vector4* data = value)
                _gl.Uniform4(LocateUniform(name, optional), (uint)value.Length, (float*)data);
        }

        public unsafe void SetUniform(string name, Plane[] value, bool optional = false)
        {

            fixed (Plane* data = value)
                _gl.Uniform4(LocateUniform(name, optional), (uint)value.Length, (float*)data);
        }


        public void SetUniform(string name, int[] value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            Span<int> span = value.AsSpan();
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

        protected string PatchShader(string sourceName, ShaderType shaderType)
        {

            StringBuilder builder = new StringBuilder();

            builder.Append("#version ")
               .Append(OpenGLRender.Current!.Options.ShaderVersion!)
               .Append('\n');

            foreach (string ext in _extensions)
                builder.Append($"#extension {ext} : require\n");

            string GetPrecision(ShaderPrecision precision) => precision switch
            {
                ShaderPrecision.Medium => "mediump",
                ShaderPrecision.High => "highp",
                ShaderPrecision.Low => "lowp",
                _ => throw new NotSupportedException()
            };


            builder.Append("precision ").Append(GetPrecision(OpenGLRender.Current!.Options.FloatPrecision)).Append(" float;\n");
            builder.Append("precision ").Append(GetPrecision(OpenGLRender.Current!.Options.IntPrecision)).Append(" int;\n");

            foreach (string feature in _features)
                builder.Append("#define ").Append(feature).Append('\n');

            if (shaderType == ShaderType.VertexShader)
                builder.Append("#define V_SHADER\n");

            PatchShader(shaderType, builder);

            Regex incRe = IncludeRegex();

            HashSet<string> included = new HashSet<string>();

            string ReplaceIncludes(string path)
            {
                string source = _resolver(path);

                while (true)
                {
                    Match match = incRe.Match(source);
                    if (!match.Success)
                        break;

                    string incName = match.Groups.Count == 3 && match.Groups[2].Length > 0 ?
                        match.Groups[2].Value :
                        match.Groups[1].Value;

                    string incPath = Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(path) ?? "", incName))
                                 .Replace('\\', '/');

                    string replace;
                    if (included.Contains(incPath))
                        replace = "";
                    else
                    {
                        included.Add(incPath);
                        replace = ReplaceIncludes(incPath);
                    }


                    source = string.Concat(
                        source.AsSpan(0, match.Index),
                        replace,
                        "\n",
                        source.AsSpan(match.Index + match.Length)
                    );
                }

                return source;
            }

            builder.Append("\n\n").Append(ReplaceIncludes(sourceName));

            return builder.ToString();
        }

        protected virtual void PatchShader(ShaderType shaderType, StringBuilder builder)
        {
        }

        public override void Dispose()
        {
            if (_handle != 0)
                _gl.DeleteProgram(_handle);

            base.Dispose();
        }

        [GeneratedRegex("#include\\s(?:(?:\"([^\"]+)\")|(?:<([^>]+)>));?\\s+")]
        protected static partial Regex IncludeRegex();
    }
}
