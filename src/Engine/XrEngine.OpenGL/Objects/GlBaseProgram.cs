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
using System.Text.Json;
using Common.Interop;

namespace XrEngine.OpenGL
{
    public abstract partial class GlBaseProgram : GlObject, IUniformProvider, IFeatureList
    {
        static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            IncludeFields = true,
            WriteIndented = true,
        };

        protected class ProgramMeta
        {
            public string? Name;
            public string? VSource;
            public string? FSource;
            public IList<string>? Features;
            public GLEnum Format;
        }

        protected readonly List<string> _features = [];
        protected readonly List<string> _mergedFetaures = [];
        protected readonly List<string> _dynamicFeatures = [];
        protected readonly List<string> _extensions = [];
        protected readonly GlRenderOptions _glOptions;
        protected readonly Func<string, string?> _resolver;
        protected readonly Dictionary<string, object> _values = [];
        protected readonly Dictionary<string, int> _locations = [];
        protected readonly Dictionary<string, string> _slots = [];
        protected Dictionary<ShaderType, HashSet<string>>? _includes;
        protected readonly int[] _boundBuffers = new int[32];
        protected readonly bool _cacheUniforms;

        protected ulong _sourceHash;


        public GlBaseProgram(GL gl, Func<string, string?> includeResolver) : base(gl)
        {
            _glOptions = OpenGLRender.Current?.Options ?? throw new InvalidOperationException("No active OpenGLRender");

            _resolver = includeResolver;
            _cacheUniforms = _glOptions.CacheUniforms == true;

            _features.EnsureCapacity(64);

        }

        public byte[] GetBinary(out GLEnum format)
        {
            _gl.GetProgram(_handle, ProgramPropertyARB.ProgramBinaryLength, out var size);

            var buffer = new byte[size];

            _gl.GetProgramBinary(_handle, out var _, out format, buffer);

            return buffer;
        }

        public void Load(byte[] binary, GLEnum format)
        {
            if (_handle != 0)
                throw new InvalidOperationException();

            _handle = _gl.CreateProgram();

            SetLabel(_label);

            _gl.ProgramBinary(
                 _handle,
                 format,
                 binary,
                 (uint)binary.Length);

            Check();
        }

        protected virtual void UpdateSourceHash()
        {

        }

        public void ClearCache()
        {
            _values.Clear();
            _locations.Clear();

            for (var i = 0; i < _boundBuffers.Length; i++)
                _boundBuffers[i] = 0;
        }

        protected bool WriteCache(string cachePath)
        {
            var cacheName = Path.Combine(cachePath, _sourceHash + ".bin");

            var data = GetBinary(out var format);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cacheName)!);

                File.WriteAllBytes(cacheName!, data);

                var meta = new ProgramMeta
                {
                    Name = _label,
                    Format = format,
                    Features = _features
                };

                UpdateMeta(meta);

                var json = JsonSerializer.Serialize(meta, JSON_OPTIONS);

                File.WriteAllText(cacheName + ".meta.json", json);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warn(this, "Error writing cache in'{0}':\n{1}", cacheName, ex);

            }
            return false;
        }

        protected virtual void UpdateMeta(ProgramMeta meta)
        {

        }

        protected bool TryReadCache(string cachePath)
        {

            var cacheName = Path.Combine(cachePath, _sourceHash + ".bin");

            if (File.Exists(cacheName))
            {
                try
                {
                    var json = File.ReadAllText(cacheName + ".meta.json");

                    var meta = JsonSerializer.Deserialize<ProgramMeta>(json, JSON_OPTIONS)!;
                    ;
                    var data = File.ReadAllBytes(cacheName);

                    Load(data, meta.Format);

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(this, "Error loading program '{0}': {1}", cacheName, ex);
                    File.Delete(cacheName);
                }
            }

            return false;
        }

        public abstract bool Build(string? cachePath = null, Func<ulong, bool>? validateHash = null);

        protected void Check()
        {
            if (_gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus) == 0)
            {
                var log = _gl.GetProgramInfoLog(_handle);
                throw new Exception(log);
            }
        }

        protected virtual void Create(params uint[] shaders)
        {
            _handle = _gl.CreateProgram();

            SetLabel(_label);

            GlDebug.Log(this, "CreateProgram {0}", _handle);

            foreach (var shader in shaders.Where(a => a != 0))
                _gl.AttachShader(_handle, shader);

            _gl.LinkProgram(_handle);

            Check();

            foreach (var shader in shaders.Where(a => a != 0))
                _gl.DetachShader(_handle, shader);

        }

        public void Use()
        {
            GlState.Current.SetActiveProgram(this);

            GlDebug.Log(this, "UseProgram {0}", _handle);
        }

        public void Unbind()
        {
            GlState.Current.SetActiveProgram(0);

            GlDebug.Log(this, "UseProgram NULL");
        }

        protected IEnumerable<string> GetUniformNames()
        {

#if GLES
            return [];
#else
            _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out var count);

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
            if (!_cacheUniforms)
                return true;

            var isChanged = false;

            if (!_values.TryGetValue(name, out var lastValue))
                isChanged = true;

            if (value is Array curArray)
            {
                var lastArray = (Array?)lastValue;

                if (!isChanged)
                {
                    if (lastArray == null || lastArray.Length != curArray.Length)
                        isChanged = true;
                    else
                    {
                        var elSize = MarshalCache.SizeOf(lastArray.GetType()!.GetElementType()!);

                        var b1 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(lastArray), lastArray.Length * elSize);
                        var b2 = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(curArray), curArray.Length * elSize);

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

        public void LoadSampler(TextureSampler value, int slot = 0)
        {
            var glSamp = value.ToGlSampler();

            GlState.Current.BindSampler(glSamp, slot);

            var isSamplerUpdate = value.Version != glSamp.Version;

            if (isSamplerUpdate)
                glSamp.Update(value);
        }

        public void LoadImage(Texture2D tex2d, int slot, BufferAccessMode accessMode = BufferAccessMode.ReadWrite)
        {
            if (slot == -1)
                throw new InvalidOperationException();

            if (!ObjectBinder.TryGet(tex2d, out GlTexture? glText))
                glText = tex2d.ToGlTexture();

            var isTexUpdate = tex2d.Version != glText.Version && tex2d.Width > 0 && tex2d.Height > 0;

            if (isTexUpdate)
                glText.Update(tex2d);

            var layered = glText.Target == TextureTarget.Texture2DArray ||
                           glText.Target == TextureTarget.Texture2DMultisampleArray;

            var glMode = accessMode switch
            {
                BufferAccessMode.Read => BufferAccessARB.ReadOnly,
                BufferAccessMode.Write => BufferAccessARB.WriteOnly,
                BufferAccessMode.Replace => BufferAccessARB.WriteOnly,
                BufferAccessMode.ReadWrite => BufferAccessARB.ReadWrite,
                _ => throw new ArgumentOutOfRangeException(nameof(accessMode))
            };

            _gl.BindImageTexture((uint)slot, glText.Handle, 0, layered, 0, glMode, glText.InternalFormat);
        }


        public void LoadTexture(Texture value, int slot, bool forceBinding = false)
        {
            if (slot == -1)
                throw new InvalidOperationException();

            var tex2d = value as Texture2D ?? throw new NotSupportedException();

            if (tex2d.Type == TextureType.Buffer)
            {
                if (!ObjectBinder.TryGet(tex2d, out GlTextureBuffer? glTextBuf))
                {
                    glTextBuf = new GlTextureBuffer(_gl);
                    ObjectBinder.Bind(tex2d, glTextBuf);
                }

                var isUpdate = tex2d.Version != glTextBuf.Version && tex2d.Data != null && tex2d.Data.Count > 0;

                if (isUpdate)
                    glTextBuf.Update(tex2d.Data![0]);

                GlState.Current.LoadTexture(glTextBuf.Texture, slot, forceBinding);

                glTextBuf.Version = tex2d.Version;
            }
            else
            {
                if (!ObjectBinder.TryGet(tex2d, out GlTexture? glText))
                    glText = tex2d.ToGlTexture();

                if (tex2d.Sampler != null)
                {
                    if (glText.Sampler == null || glText.Sampler.Source != tex2d.Sampler)
                        glText.Sampler = tex2d.Sampler.ToGlSampler();

                    LoadSampler(tex2d.Sampler, slot);
                }
                else
                    glText.Sampler = null;

                var isTexUpdate = tex2d.Version != glText.Version && tex2d.Width > 0 && tex2d.Height > 0;

                GlState.Current.LoadTexture(glText, slot, forceBinding);

                if (isTexUpdate)
                    glText.Update(tex2d);
            }
        }

        public void SetUniform(string name, bool value, bool optional = false)
        {
            SetUniform(name, value ? 1 : 0, optional);
        }

        public void SetUniform(string name, int value, bool optional = false, bool force = false)
        {
            if (!force && !IsChanged(name, value))
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

        public void LoadBuffer<T>(ISimpleBuffer<T> buffer, int slot = 0, BufferUsage usage = BufferUsage.Default)
        {
            if (buffer is GlBufferRangeSlot<T> rangeBuf)
            {
                rangeBuf.Load(this);
            }
            else
            {
                var glBuffer = (IGlBuffer)buffer;

                var curTarget = usage switch
                {
                    BufferUsage.SSbo => BufferTargetARB.ShaderStorageBuffer,
                    BufferUsage.Uniforms => BufferTargetARB.UniformBuffer,
                    _ => glBuffer.Target
                };

                GlState.Current.LoadBuffer(glBuffer, slot, curTarget);
            }
        }

        public void SetUniform(string name, Texture value, int slot = 0, bool optional = false)
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
            var span = value.AsSpan();
            _gl.Uniform1(LocateUniform(name, optional), span);
        }

        public void SetUniform(string name, Vector2I value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform2(LocateUniform(name, optional), value.X, value.Y);
        }

        public void SetUniform(string name, Vector3I value, bool optional = false)
        {
            if (!IsChanged(name, value))
                return;
            _gl.Uniform3(LocateUniform(name, optional), value.X, value.Y, value.Z);
        }

        public void AddDynamicFeature(string name)
        {
            _dynamicFeatures.Add(name);
        }

        public void AddFeature(string name)
        {
            _features.Add(name);
        }

        public void AddExtension(string name)
        {
            _extensions.Add(name);
        }

        public void SetSlot(string name, string value)
        {
            _slots[name] = value;
        }

        public void Include(string inc, ShaderType shaderType)
        {
            _includes ??= [];
            if (!_includes.TryGetValue(shaderType, out var list))
            {
                list = new HashSet<string>();
                _includes[shaderType] = list;
            }
            list.Add(inc);
        }

        protected string PatchShader(string sourceName, ShaderType shaderType)
        {
            var builder = new StringBuilder();

            builder.Append("#version ")
               .Append(_glOptions.ShaderVersion!)
               .Append('\n');

            foreach (var ext in _extensions)
                builder.Append($"#extension {ext} : require\n");

            string GetPrecision(ShaderPrecision precision) => precision switch
            {
                ShaderPrecision.Medium => "mediump",
                ShaderPrecision.High => "highp",
                ShaderPrecision.Low => "lowp",
                _ => throw new NotSupportedException()
            };

            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler2DShadow;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler2DMSArray;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler2DMS;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler2DArray;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler2D;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" sampler3D;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.SamplerPrecision)).Append(" samplerCube;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.FloatPrecision)).Append(" float;\n");
            builder.Append("precision ").Append(GetPrecision(_glOptions.IntPrecision)).Append(" int;\n");

            _mergedFetaures.Clear();
            _mergedFetaures.AddRange(_features);

            if (shaderType == ShaderType.VertexShader)
                _mergedFetaures.Add("VERTEX_SHADER");

            else if (shaderType == ShaderType.FragmentShader)
                _mergedFetaures.Add("FRAGMENT_SHADER");

            if (!_glOptions.UseShaderPreprocessor)
            {
                foreach (var feature in _mergedFetaures)
                    builder.Append("#define ").Append(feature).Append('\n');

                if (_includes != null && _includes.TryGetValue(shaderType, out var includes))
                {
                    builder.AppendLine();

                    foreach (var inc in includes)
                        builder.Append("#include \"").Append(inc).Append('\"').AppendLine();

                    builder.AppendLine();
                }

                foreach (var slot in ResourceSlot.Enumerate(typeof(TextureSlots)))
                {
                    if (slot.Slot == -1 || slot.Name == null)
                        continue;

                    builder.Append("#ifndef ").Append(slot.Name).Append('\n');
                    builder.Append("#define ").Append(slot.Name).Append(' ').Append(slot.Slot).Append('\n');
                    builder.Append("#endif\n");
                }
            }
            else
            {
                foreach (var slot in ResourceSlot.Enumerate(typeof(TextureSlots)).Where(a=> a.Slot != -1))
                    _mergedFetaures.Add($"{slot.Name} {slot.Slot}");
            }

            PatchShader(shaderType, builder);

            if (_glOptions.UseShaderPreprocessor)
            {
                var preProc = new GlslPreprocessor(_resolver!);
#if DEBUG
                preProc.EmitConditionComments = false;
#endif
                var runDefine = _dynamicFeatures.Count == 0 ? null : _dynamicFeatures
                    .Select(a => new GlslRuntimeDefine(a, a))
                    .ToArray();

                if (_includes == null || !_includes.TryGetValue(shaderType, out var includes))
                    includes = null;

                var source = preProc.Process(sourceName, new GlslPreprocessorOptions
                {
                    Defines = _mergedFetaures,
                    RuntimeDefines = runDefine,
                    Slots = _slots,
                    IncludeFiles = includes,
                    AllowRedefine = true
                });

                builder.Append(source);
            }
            else
            {
                if (_dynamicFeatures.Count > 0)
                    throw new InvalidOperationException("Use of dynamic fetaures with 'UseShaderPreprocessor' off");

                var incRe = IncludeRegex();

                var included = new HashSet<string>();

                string ReplaceIncludes(string path)
                {
                    var source = _resolver(path);

                    if (string.IsNullOrEmpty(source))
                        throw new InvalidOperationException($"include source '{path}' not found ");

                    while (true)
                    {
                        var match = incRe.Match(source);
                        if (!match.Success)
                            break;

                        var incName = match.Groups.Count == 3 && match.Groups[2].Length > 0 ?
                            match.Groups[2].Value :
                            match.Groups[1].Value;

                        var incPath = Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(path) ?? "", incName))
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
            }

            return builder.ToString();
        }

        public bool Validate()
        {
            _gl.ValidateProgram(_handle);

            _gl.GetProgram(_handle, ProgramPropertyARB.ValidateStatus, out var ok);

            if (ok == 0)
            {
                var log = _gl.GetProgramInfoLog(_handle);
                Log.Warn(this, log);
                return false;
            }

            return true;
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

        public ulong SourceHash => _sourceHash;

    }
}
