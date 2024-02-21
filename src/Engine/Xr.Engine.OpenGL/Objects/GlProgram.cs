#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Reflection;
using System.Collections;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;


namespace OpenXr.Engine.OpenGL
{
    public abstract partial class GlProgram : GlObject, IUniformProvider, IFeatureList
    {
        protected readonly Dictionary<string, int> _locations = [];
        protected readonly GlRenderOptions _options;
        protected readonly HashSet<string> _features = [];
        protected readonly HashSet<string> _extensions = [];
        protected readonly Func<string, string> _resolver;
        protected string _programId = "";

        protected string _currentHash = string.Empty;

        readonly protected static Dictionary<string, uint> _programsCache = [];

        public GlProgram(GL gl, Func<string, string> includeResolver, GlRenderOptions options) : base(gl)
        {
            _options = options;
            _resolver = includeResolver;
        }

        public virtual void BeginEdit()
        {
            _features.Clear();
            _extensions.Clear();
        }

        protected virtual void Build()
        {

        }

        public virtual void Commit()
        {
            var hash = _features.Count == 0 ?
                        _programId :
                        string.Concat(_programId, ":", Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(string.Join(',', _features)))));

            if (hash != _currentHash)
            {
                if (!_programsCache.TryGetValue(hash, out var progId))
                {
                    Build();
                    _programsCache[hash] = _handle;  
                }
                else
                    _handle = progId;   

                _currentHash = hash;
            }
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

        protected int LocateUniform(string name, bool optional = false, bool isBlock = false)
        {
            if (!_locations.TryGetValue(name, out var result))
            {
                if (isBlock)
                    result = (int)_gl.GetUniformBlockIndex(_handle, name);
                else
                    result = _gl.GetUniformLocation(_handle, name);
                if (result == -1 && !optional)
                {
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

        public void SetUniform(string name, int value, bool optional = false)
        {
            _gl.Uniform1(LocateUniform(name, optional), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value, bool optional = false)
        {
            _gl.UniformMatrix4(LocateUniform(name, optional), 1, false, (float*)&value);
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

        public void SetUniformBuffer<T>(string name, GlBuffer<T> buffer, bool optional = false) where T : unmanaged
        {
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)LocateUniform(name, false, true), buffer.Handle);
        }

        public unsafe void SetUniform(string name, Texture2D value, int slot = 0, bool optional = false)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + slot);

            var texture = value.GetResource(a => value.CreateGlTexture(_gl, _options.RequireTextureCompression));

            texture.Bind();

            SetUniform(name, slot, optional);
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
               .Append(_options.ShaderVersion!)
               .Append('\n');

            foreach (var ext in _extensions)
                builder.Append($"#extension {ext} : require\n");

            var precision = _options.FloatPrecision switch
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
            _gl.DeleteProgram(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex("#include\\s(?:(?:\"([^\"]+)\")|(?:<([^>]+)>));?\\s+")]
        protected static partial Regex IncludeRegex();

    }
}
