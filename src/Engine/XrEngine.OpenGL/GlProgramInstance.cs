using System.Diagnostics;

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public partial class GlProgramInstance : IBufferProvider, IDisposable
    {
        static internal readonly Dictionary<string, GlBaseProgram> _programs = [];

        protected ShaderUpdate? _materialUpdate;
        protected ShaderUpdate? _modelUpdate;
        protected readonly GL _gl;

        protected long _materialVersion = -1;
        protected long _globalVersion = -1;
        protected long _lastGlobalContextVersion = -1;

        protected IGlBuffer?[] _materialBuffers;
        protected IGlBuffer?[] _modelBuffers;


        public GlProgramInstance(GL gl, ShaderMaterial material, GlProgramGlobal global, Object3D? model)
        {
            _gl = gl;
            Material = material;
            Global = global;

            GlBufferMap bufferMap = material.GetOrCreateProp(OpenGLRender.Props.BufferMap, () => new GlBufferMap(10));
            _materialBuffers = bufferMap.Buffers;

            if (model != null)
            {
                bufferMap = model.GetOrCreateProp(OpenGLRender.Props.BufferMap, () => new GlBufferMap(10));
                _modelBuffers = bufferMap.Buffers;
            }
            else
                _modelBuffers = [];

        }

        public void UpdateModel(UpdateShaderContext ctx)
        {
            Debug.Assert(ctx.Stage == UpdateShaderStage.Model);

            GlBufferMap bufferMap = ctx.Model!.GetOrCreateProp(OpenGLRender.Props.BufferMap, () => new GlBufferMap(10));

            _modelBuffers = bufferMap.Buffers;

            if (_modelUpdate == null)
            {
                ShaderUpdateBuilder localBuilder = new ShaderUpdateBuilder(ctx);

                Material!.UpdateShader(localBuilder);

                if (Global.Shader is IShaderHandler handler)
                    handler.UpdateShader(localBuilder);

                _modelUpdate = localBuilder.Result;
            }

            UpdateBuffers(ctx);

            UpdateUniforms(ctx, false);
        }

        public bool UpdateProgram(UpdateShaderContext ctx)
        {
            if (Program != null && _materialVersion == Material!.Version && _globalVersion == Global.Version)
                return false;

            ctx.BufferProvider = this;

            ShaderUpdateBuilder localBuilder = new ShaderUpdateBuilder(ctx);
            Material!.UpdateShader(localBuilder);

            foreach (string feature in Global.ShaderUpdate!.Features!)
                localBuilder.AddFeature(feature);

            if (ExtraFeatures != null)
            {
                foreach (string feature in ExtraFeatures)
                    localBuilder.AddFeature(feature);
            }

            Shader shader = Material.Shader!;

            TessellationMode tesMode = Material is ITessellationMaterial tes ? tes.TessellationMode : TessellationMode.None;

            bool useTess = shader.TessEvalSourceName != null && tesMode != TessellationMode.None;

            bool useGeo = shader.GeometrySourceName != null &&
                         (shader.TessEvalSourceName == null || tesMode == TessellationMode.Geometry);

            if (useTess)
                localBuilder.AddFeature("USE_TESS_SHADER");

            if (useGeo)
                localBuilder.AddFeature("USE_GEO_SHADER");

            localBuilder.AddFeature($"SHADER_VER {shader.Version}");

            localBuilder.ComputeHash(Material.GetType().FullName!);

            _materialUpdate = localBuilder.Result;

            if (!_programs.TryGetValue(_materialUpdate.FeaturesHash!, out GlBaseProgram? program))
            {
                Func<string, string> resolver = name =>
                {
                    if (shader.SourcePaths != null && shader.SourcePaths.Length > 0)
                    {
                        string? fullPath = shader.SourcePaths.
                                       Select(a => Path.Combine(a, name))
                                       .Where(File.Exists)
                                        .FirstOrDefault();
                        if (fullPath != null)
                            return File.ReadAllText(fullPath);
                    }
                    return shader.Resolver!(name);
                };

                program = new GlSimpleProgram(_gl,
                    shader.VertexSourceName!,
                    shader.FragmentSourceName!,
                    useGeo ? shader.GeometrySourceName : null,
                    useTess ? shader.TessControlSourceName : null,
                    useTess ? shader.TessEvalSourceName : null,
                    resolver);

                if (useGeo)
                {
                    program.AddExtension("GL_EXT_geometry_shader");
                    program.AddExtension("GL_OES_geometry_shader");
                }

                if (useTess)
                {
                    program.AddExtension("GL_EXT_tessellation_shader");
                    program.AddExtension("GL_OES_tessellation_shader");
                }

                if (ExtraExtensions != null)
                {
                    foreach (string ext in ExtraExtensions)
                        program.AddExtension(ext);
                }

                foreach (string ext in _materialUpdate.Extensions!)
                    program.AddExtension(ext);

                foreach (string feature in _materialUpdate.Features!)
                    program.AddFeature(feature);

                foreach (string ext in Global.ShaderUpdate!.Extensions!)
                    program.AddExtension(ext);

                /*
                foreach (var feature in Global.Update!.Features!)
                    program.AddFeature(feature);
                */

                //TODO not working
                //program.AddFeature("ZLOG_F 0.0001");

                program.Build();

                _programs[_materialUpdate.FeaturesHash!] = program;
            }

            bool changed = Program == null || program.Handle != Program.Handle;

            program.Use();

            Program = program;

            _materialVersion = Material.Version;
            _globalVersion = Global.Version;

            return changed;
        }

        public IBuffer<T> GetBuffer<T>(int bufferId, BufferStore store)
        {
            if (store == BufferStore.Shader)
                return Global.GetBuffer<T>(bufferId, store);

            IGlBuffer?[] storeBuffers = store == BufferStore.Material ? _materialBuffers : _modelBuffers;

            if (storeBuffers.Length == 0)
                throw new NotSupportedException("Buffer store not supported");

            IBuffer<T>? buffer = (IBuffer<T>?)storeBuffers[bufferId];
            if (buffer == null)
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                storeBuffers[bufferId] = (IGlBuffer)buffer;
            }
            return buffer;
        }

        public void UpdateBuffers(UpdateShaderContext ctx, bool updateGlobals = false)
        {
            ShaderUpdate? update = ctx.Stage == UpdateShaderStage.Any ||
                         ctx.Stage == UpdateShaderStage.Material ? _materialUpdate : _modelUpdate;

            if (update == null)
                return;

            ctx.BufferProvider = this;

            if (updateGlobals)
                Global.UpdateBuffers(ctx);

            foreach (UpdateBufferAction action in update.BufferUpdates!)
                action(ctx);
        }


        public void UpdateUniforms(UpdateShaderContext ctx, bool updateGlobals)
        {
            ShaderUpdate? update = ctx.Stage == UpdateShaderStage.Any ||
                         ctx.Stage == UpdateShaderStage.Material ? _materialUpdate : _modelUpdate;

            if (update == null)
                return;

            ctx.BufferProvider = this;

            if (updateGlobals)
            {
                //TODO: unsure that ContextVersion change when Camera or Lights changes
                if (ctx.ContextVersion != _lastGlobalContextVersion)
                {
                    Global.UpdateUniforms(ctx, Program!);
                    _lastGlobalContextVersion = ctx.ContextVersion;
                }
            }

            foreach (UpdateUniformAction action in update.Actions!)
                action(ctx, Program!);
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

        public string[]? ExtraFeatures { get; set; }

        public string[]? ExtraExtensions { get; set; }

        public GlProgramGlobal Global { get; }

        public ShaderMaterial Material { get; }

        public GlBaseProgram? Program { get; set; }
    }
}
