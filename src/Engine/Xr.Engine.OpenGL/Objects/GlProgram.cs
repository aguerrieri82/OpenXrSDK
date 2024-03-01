﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System;


namespace Xr.Engine.OpenGL
{
    public abstract partial class GlProgram : GlObject, IUniformProvider, IFeatureList
    {
        protected Dictionary<string, int>? _curLocations;
        protected readonly Dictionary<uint, Dictionary<string, int>> _handleLocations = [];
        protected readonly List<string> _features = [];
        protected readonly List<string> _extensions = [];
        protected readonly Func<string, string> _resolver;
        protected string _programId = "";

        protected string _currentHash = string.Empty;

        readonly protected static Dictionary<string, uint> _programsCache = [];

        public GlProgram(GL gl, Func<string, string> includeResolver) : base(gl)
        {
            _resolver = includeResolver;
        }

        public virtual void BeginEdit()
        {
            _features.Clear();
            _extensions.Clear();
            _curLocations?.Clear();
        }

        protected virtual void Build()
        {

        }

        public virtual void Commit(string hash)
        {
            if (!_programsCache.ContainsKey(hash))
            {
                Build();
                _programsCache[hash] = _handle;
                _curLocations = [];
                _handleLocations[_handle] = _curLocations;
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

        public void Use(string featureHash)
        {
            _handle = _programsCache[featureHash];
            _curLocations = _handleLocations[_handle];

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

            if (!_curLocations!.TryGetValue(name, out var result))
            {
                if (isBlock)
                {
                    result = (int)_gl.GetUniformBlockIndex(_handle, name);
                    if (result != -1)
                        _gl.UniformBlockBinding(_handle, (uint)result, (uint)result);
                }
             
                else
                    result = _gl.GetUniformLocation(_handle, name);
                if (result == -1 && !optional)
                {
                    //Debug.WriteLine($"--- WARN --- {name} NOT FOUND");
                    //throw new Exception($"{name} uniform not found on shader.");
                }

                _curLocations[name] = result;
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

        public unsafe void SetUniformBuffer<T>(string name, T data, bool optional = false, bool updateBuffer = false) 
        {
            var buffer = GetBuffer<T>(name);

            if (updateBuffer)
            {
                if (data is IDynamicBuffer dynamic)
                {
                    using var dynBuffer = dynamic.GetBuffer();
                    buffer.Update(dynBuffer.Data, dynBuffer.Size);
                }
                else
                    buffer.Update(new Span<T>(ref data));
            }
            else
                SetUniform(name, (IBuffer)buffer, optional);
        }

        protected virtual GlBuffer<T> GetBuffer<T>(string name) 
        {
            throw new NotSupportedException();
        }

        public void SetUniform(string name, IBuffer buffer, bool optional = false)
        {
            var index = (uint)LocateUniform(name, optional, true);

            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, index, ((GlObject)buffer).Handle);
        }

        public void LoadTexture(Texture2D value, int slot = 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + slot);

            var texture = value.GetResource(a => value.CreateGlTexture(_gl, OpenGLRender.Current!.Options.RequireTextureCompression));

            texture.Bind();
        }

        public unsafe void SetUniform(string name, Texture2D value, int slot = 0, bool optional = false)
        {
            LoadTexture(value, slot);

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
            _gl.DeleteProgram(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex("#include\\s(?:(?:\"([^\"]+)\")|(?:<([^>]+)>));?\\s+")]
        protected static partial Regex IncludeRegex();

      
    }
}
