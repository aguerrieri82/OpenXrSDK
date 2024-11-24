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

        protected ShaderUpdate? _update;
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

            var bufferMap = material.GetOrCreateProp("BufferMap", () => new GlBufferMap(10));
            _materialBuffers = bufferMap.Buffers;

            if (model != null)
            {
                bufferMap = model.GetOrCreateProp("BufferMap", () => new GlBufferMap(10));
                _modelBuffers = bufferMap.Buffers;
            }
            else
                _modelBuffers = [];
        }

        public bool UpdateProgram(UpdateShaderContext ctx)
        {
            if (Program != null && _materialVersion == Material!.Version && _globalVersion == Global.Version)
                return false;

            ctx.BufferProvider = this;

            var localBuilder = new ShaderUpdateBuilder(ctx);
            Material!.UpdateShader(localBuilder);

            foreach (var feature in Global.Update!.Features!)
                localBuilder.AddFeature(feature);

            if (ExtraFeatures != null)
            {
                foreach (var feature in ExtraFeatures)
                    localBuilder.AddFeature(feature);
            }

            localBuilder.ComputeHash(Material.GetType().FullName!);

            _update = localBuilder.Result;

            if (!_programs.TryGetValue(_update.FeaturesHash!, out var program))
            {
                var shader = Material.Shader!;
                var resolver = shader.Resolver!;

                program = new GlSimpleProgram(_gl,
                    shader.VertexSourceName!,
                    shader.FragmentSourceName!,
                    shader.GeometrySourceName != null ? shader.GeometrySourceName : null,
                    resolver);

                if (shader.GeometrySourceName != null)
                {
                    program.AddExtension("GL_EXT_geometry_shader");
                    program.AddExtension("GL_OES_geometry_shader");
                }

                if (ExtraExtensions != null)
                {
                    foreach (var ext in ExtraExtensions)
                        program.AddExtension(ext);
                }

                foreach (var ext in _update.Extensions!)
                    program.AddExtension(ext);

                foreach (var feature in _update.Features!)
                    program.AddFeature(feature);

                foreach (var ext in Global.Update!.Extensions!)
                    program.AddExtension(ext);

                /*
                foreach (var feature in Global.Update!.Features!)
                    program.AddFeature(feature);
                */

                //TODO not working
                //program.AddFeature("ZLOG_F 0.001");

                program.Build();

                _programs[_update.FeaturesHash!] = program;
            }

            var changed = Program == null || program.Handle != Program.Handle;

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

            var storeBuffers = store == BufferStore.Material ? _materialBuffers : _modelBuffers;

            if (storeBuffers.Length == 0)
                throw new NotSupportedException("Buffer store not supported");

            var buffer = (IBuffer<T>?)storeBuffers[bufferId];
            if (buffer == null)
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                storeBuffers[bufferId] = (IGlBuffer)buffer;
            }
            return buffer;
        }

        public void UpdateBuffers(UpdateShaderContext ctx)
        {
            foreach (var action in _update!.BufferUpdates!)
                action(ctx);
        }


        public void UpdateUniforms(UpdateShaderContext ctx, bool updateGlobals)
        {
            ctx.BufferProvider = this;

            if (updateGlobals)
            {
                if (ctx.ContextVersion != _lastGlobalContextVersion)
                {
                    Global.UpdateUniforms(ctx, Program!);
                    _lastGlobalContextVersion = ctx.ContextVersion;
                }
            }

            foreach (var action in _update!.Actions!)
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
