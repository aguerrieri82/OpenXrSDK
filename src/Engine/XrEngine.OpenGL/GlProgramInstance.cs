using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using XrEngine.Helpers;
using System.Collections.Concurrent;


#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

namespace XrEngine.OpenGL
{
    public partial class GlProgramInstance : IBufferProvider, IDisposable
    {
        public static int MAX_BUFFERS = 32;
        static internal readonly ConcurrentDictionary<ulong, GlSimpleProgram> _programs = [];
        static readonly GlSharedWorker _worker = new();

        protected ShaderUpdate? _materialUpdate;
        protected ShaderUpdate? _modelUpdate;
        protected readonly GL _gl;

        protected long _materialVersion = -1;
        protected long _globalVersion = -1;
        protected long _lastGlobalContextVersion = -1;

        protected IGlBuffer?[] _materialBuffers;
        protected IGlBuffer?[] _modelBuffers;

        protected Object3D? _lastModel;
        private bool _useTess;
        private bool _useGeo;
        private Task? _createTask;

        private readonly bool _useShaderCache;


        public GlProgramInstance(GL gl, ShaderMaterial material, GlProgramGlobal global, Object3D? model)
        {
            _gl = gl;
            _useShaderCache = OpenGLRender.Current!.Options.UseShaderCache;

            Material = material;
            Global = global;

            if (_useShaderCache)
                CachePath ??= Path.Combine(Context.Require<IPlatform>().SharedPath, "Cache", "Shaders");

            var bufferMap = material.GetOrCreateProp(OpenGLRender.Props.BufferMap, () => new GlBufferMap<IGlBuffer>(MAX_BUFFERS, material));
            _materialBuffers = bufferMap.Buffers;

            if (model != null)
                LoadModelBuffers(model);
            else
                _modelBuffers = [];
        }

        [MemberNotNull(nameof(_modelBuffers))]
        protected void LoadModelBuffers(Object3D model)
        {
            var bufferMap = model.GetOrCreateProp(OpenGLRender.Props.BufferMap, () => new GlBufferMap<IGlBuffer>(MAX_BUFFERS, model));
            _modelBuffers = bufferMap.Buffers;

            _lastModel = model;
        }

        public void UpdateModel(UpdateShaderContext ctx)
        {
            Debug.Assert(ctx.Stage == UpdateShaderStage.Model && ctx.Model != null);

            LoadModelBuffers(ctx.Model);

            if (_modelUpdate == null)
            {
                var localBuilder = new ShaderUpdateBuilder(ctx);

                Material.UpdateShader(localBuilder);

                if (Global.Shader is IShaderHandler handler)
                    handler.UpdateShader(localBuilder);

                _modelUpdate = localBuilder.Result;
            }

            UpdateBuffers(ctx);

            UpdateUniforms(ctx, false);
        }


        public bool UpdateProgram(UpdateShaderContext ctx)
        {
            if (_createTask != null)
            {
                if (!_createTask.IsCompleted)
                    return false;

                if (_createTask.IsFaulted)
                    _createTask.GetAwaiter().GetResult();

                _createTask = null;
            }

            if (!NeedUpdate)
                return false;

            ctx.BufferProvider = this;

            var localBuilder = new ShaderUpdateBuilder(ctx);

            Material.UpdateShader(localBuilder);

            if (Global.ShaderUpdate?.Features != null)
            {
                foreach (var feature in Global.ShaderUpdate.Features)
                    localBuilder.AddFeature(feature);
            }

            if (ExtraFeatures != null)
            {
                foreach (var feature in ExtraFeatures)
                    localBuilder.AddFeature(feature);
            }

            var shader = Material.Shader;

            var tesMode = Material is ITessellationMaterial tes ? tes.TessellationMode : TessellationMode.None;

            _useTess = shader.TessEvalSourceName != null && tesMode != TessellationMode.None;

            _useGeo = shader.GeometrySourceName != null &&
                         (shader.TessEvalSourceName == null || tesMode == TessellationMode.Geometry);

            if (_useTess)
                localBuilder.AddFeature("USE_TESS_SHADER");

            if (_useGeo)
                localBuilder.AddFeature("USE_GEO_SHADER");

            localBuilder.ComputeHash(Material.GetType().FullName!);

            _materialUpdate = localBuilder.Result;

            if (!_programs.TryGetValue(_materialUpdate.FeaturesHash, out var program))
            {
                if (UseWorker)
                {
                    if (!_worker.IsStarted)
                        _worker.Start();

                    _createTask = _worker.Dispatcher.ExecuteAsync(() =>
                    {
                        program = CreateProgram();

                        _programs[_materialUpdate.FeaturesHash] = program;
                    });

                    return false;
                }

                program = CreateProgram();
 
                _programs[_materialUpdate.FeaturesHash] = program;
            }

            var changed = Program == null || program.Handle != Program.Handle;

            program.Use();

            Program = program;

            _materialVersion = Material.Version;
            _globalVersion = Global.Version;

            return changed;
        }

        protected GlSimpleProgram CreateProgram()
        {
            var shader = Material.Shader;

            string? Resolver(string name)
            {
                if (shader.SourcePaths != null && shader.SourcePaths.Length > 0)
                {
                    var fullPath = shader.SourcePaths.
                                   Select(a => Path.Combine(a, name))
                                  .FirstOrDefault(File.Exists);

                    if (fullPath != null)
                        return File.ReadAllText(fullPath);
                }

                var result = shader.Resolver?.Invoke(name);

                if (!string.IsNullOrEmpty(result))
                    return result;

                return Material.Resolver?.Invoke(name);
            }

            var program = new GlSimpleProgram(_gl,
                shader.VertexSourceName!,
                shader.FragmentSourceName!,
                _useGeo ? shader.GeometrySourceName : null,
                _useTess ? shader.TessControlSourceName : null,
                _useTess ? shader.TessEvalSourceName : null,
                Resolver)
            {
                Source = Material
            };

            program.SetLabel(Material.GetType().Name);

            if (_useGeo)
            {
                program.AddExtension("GL_EXT_geometry_shader");
                program.AddExtension("GL_OES_geometry_shader");
            }

            if (_useTess)
            {
                program.AddExtension("GL_EXT_tessellation_shader");
                program.AddExtension("GL_OES_tessellation_shader");
            }

            if (ExtraExtensions != null)
            {
                foreach (var ext in ExtraExtensions)
                    program.AddExtension(ext);
            }

            if (_materialUpdate!.Extensions != null)
            {
                foreach (var ext in _materialUpdate.Extensions)
                    program.AddExtension(ext);
            }

            if (_materialUpdate.Features != null)
            {
                foreach (var feature in _materialUpdate.Features)
                    program.AddFeature(feature);
            }

            if (Global.ShaderUpdate?.Extensions != null)
            {
                foreach (var ext in Global.ShaderUpdate.Extensions)
                    program.AddExtension(ext);
            }

            program.Build(CachePath);

            return program;
        }

        public ISimpleBuffer<T> GetBuffer<T>(int bufferId, BufferStore store, BufferUsage usage, string? uniformName = "")
        {
            if (store == BufferStore.Shader)
                return Global.GetBuffer<T>(bufferId, store, usage);

            if (usage == BufferUsage.SharedSsbo)
            {
                Debug.Assert(uniformName != null && _lastModel != null);

                var range = Global.GetBufferRange<T>(bufferId, store, uniformName);

                EngineObject engObj = store == BufferStore.Material ? Material : _lastModel;

                var buffer = range.Reserve(engObj);

                return buffer;
            }
            else
            {
                var storeBuffers = store == BufferStore.Material ? _materialBuffers : _modelBuffers;

                if (storeBuffers.Length == 0)
                    throw new NotSupportedException("Buffer store not supported");

                var buffer = (GlBuffer<T>?)storeBuffers[bufferId];
                if (buffer == null)
                {
                    var target = usage == BufferUsage.SSbo ? BufferTargetARB.ShaderStorageBuffer : BufferTargetARB.UniformBuffer;
                    buffer = new GlBuffer<T>(_gl, target);
                    storeBuffers[bufferId] = buffer;
                }

                return buffer;
            }
        }

        public void UpdateBuffers(UpdateShaderContext ctx, bool updateGlobals = false)
        {
            var update = ctx.Stage == UpdateShaderStage.Any ||
                         ctx.Stage == UpdateShaderStage.Material ? _materialUpdate : _modelUpdate;

            if (update?.BufferUpdates == null)
                return;

            ctx.BufferProvider = this;

            if (updateGlobals)
                Global.UpdateBuffers(ctx);

            foreach (var action in update.BufferUpdates)
                action(ctx);
        }

        public void UpdateUniforms(UpdateShaderContext ctx, bool updateGlobals)
        {
            Debug.Assert(Program != null);

            var update = ctx.Stage == UpdateShaderStage.Any ||
                         ctx.Stage == UpdateShaderStage.Material ? _materialUpdate : _modelUpdate;

            if (update == null)
                return;

            ctx.BufferProvider = this;

            if (updateGlobals)
            {
                //TODO: unsure that ContextVersion change when Camera or Lights changes
                if (ctx.ContextVersion != _lastGlobalContextVersion)
                {
                    Global.UpdateUniforms(ctx, Program);
                    _lastGlobalContextVersion = ctx.ContextVersion;
                }
            }

            foreach (var action in update.Actions)
                action(ctx, Program);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Invalidate()
        {
            _materialVersion = -1;
            _globalVersion = -1;
        }

        public bool IsReady => Program != null && Program.Handle != 0;

        public bool NeedUpdate => Program == null || _materialVersion != Material.Version || _globalVersion != Global.Version;

        public string[]? ExtraFeatures { get; set; }

        public string[]? ExtraExtensions { get; set; }

        public GlProgramGlobal Global { get; }

        public ShaderMaterial Material { get; }

        public GlBaseProgram? Program { get; set; }

        public static string? CachePath { get; set; }

        public bool UseWorker { get; set; }

    }
}
