#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text.RegularExpressions;
using System.Text;
using XrMath;


namespace XrEngine.OpenGL
{
    public abstract partial class GlBaseProgram : GlObject, IUniformProvider, IFeatureList
    {
        protected readonly Dictionary<string, int> _locations = [];
        protected readonly List<string> _features = [];
        protected readonly List<string> _extensions = [];
        protected readonly Func<string, string> _resolver;
        protected readonly Dictionary<string, int> _boundTextures = [];
        protected readonly ushort[] _boundBuffers = new ushort[256];

        public GlBaseProgram(GL gl, Func<string, string> includeResolver) : base(gl)
        {
            _resolver = includeResolver;
        }

        public abstract void Build();

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
                _gl.DetachShader(_handle, shader);
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

        protected IEnumerable<string> GetUniformNames()
        {
            uint i = 0;

            _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out int count);

#if GLES
            return [];
#else

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

        public void SetUniform(string name, int value, bool optional = false)
        {
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public void SetUniform(string name, uint value, bool optional = false)
        {
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value, bool optional = false)
        {
            _gl.UniformMatrix4(LocateUniform(name, optional), 1, false, (float*)&value);
        }


        public unsafe void SetUniform(string name, Matrix3x3 value, bool optional = false)
        {
            _gl.UniformMatrix3(LocateUniform(name, optional), 1, false, (float*)&value);
        }


        public void SetUniform(string name, float value, bool optional = false)
        {
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public void SetUniform(string name, Vector3 value, bool optional = false)
        {
            _gl.Uniform3(LocateUniform(name, optional), value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Color value, bool optional = false)
        {
            _gl.Uniform4(LocateUniform(name, optional), value.R, value.G, value.B, value.A);
        }


        public void SetUniform(string name, IBuffer buffer, bool optional = false)
        {
            var glBuffer = (IGlBuffer)buffer;

            var index = (uint)LocateUniform(name, optional, true);

            if (_boundBuffers[index] != glBuffer.Slot)
            {
                _gl.UniformBlockBinding(_handle, index, glBuffer.Slot);
                _boundBuffers[index] = (ushort)glBuffer.Slot;
            }
        }

        public void LoadTexture(Texture value, int slot = 0)
        {
            var tex2d = value as Texture2D ?? throw new NotSupportedException();

            _gl.ActiveTexture(TextureUnit.Texture0 + slot);

            var texture = value.GetResource(a => tex2d.CreateGlTexture(_gl, OpenGLRender.Current!.Options.RequireTextureCompression));
            
            if (tex2d.Version != texture.Version)
                texture.Update(tex2d);

            texture.Bind();
        }

        public unsafe void SetUniform(string name, Texture value, int slot = 0, bool optional = false)
        {
            LoadTexture(value, slot);

            if (!_boundTextures.TryGetValue(name, out var curSlot) || slot != curSlot)
            {
                SetUniform(name, slot, optional);
                _boundTextures[name] = slot;
            }
        }



        public void SetUniform(string name, float[] obj, bool optional = false)
        {
            var span = obj.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public void SetUniform(string name, int[] obj, bool optional = false)
        {
            var span = obj.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public void SetUniform(string name, Vector2I value, bool optional = false)
        {
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
