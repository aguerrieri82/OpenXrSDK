#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlProgramInstance : IBufferProvider
    {
        static readonly Dictionary<string, GlBaseProgram> _programs = [];

        protected Dictionary<string, IGlBuffer> _buffers = [];
        protected ShaderUpdate? _update;
        protected readonly GL _gl;
        protected long _programVersion = -1;

        public GlProgramInstance(GL gl, ShaderMaterial material, GlProgramGlobal global)
        {
            _gl = gl;
            Material = material;
            Global = global;
        }

        public void UpdateProgram(UpdateShaderContext ctx)
        {
            if (Program != null && _programVersion == Material!.Version)
                return;

            ctx.BufferProvider = this;

            var localBuilder = new ShaderUpdateBuilder(ctx);
            Material!.UpdateShader(localBuilder);

            localBuilder.ComputeHash(Material.GetType().FullName!);

            _update = localBuilder.Result;

            if (!_programs.TryGetValue(_update.FeaturesHash!, out var program))
            {
                var shader = Material.Shader!;
                var resolver = shader.Resolver!;

                program = new GlSimpleProgram(_gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver);

                foreach (var ext in _update.Extensions!)
                    program.AddExtension(ext);

                foreach (var feature in _update.Features!)
                    program.AddFeature(feature);

                foreach (var ext in Global.Update!.Extensions!)
                    program.AddExtension(ext);

                foreach (var feature in Global.Update!.Features!)
                    program.AddFeature(feature);

                program.Build();

                _programs[_update.FeaturesHash!] = program;
            }

            program.Use();

            foreach (var action in _update!.BufferUpdates!)
                action(ctx);

            Program = program;

            _programVersion = Material.Version;
        }

        public IBuffer GetBuffer<T>(string name, bool isGlobal)
        {
            if (isGlobal)
                return Global.GetBuffer<T>(name, true);

            if (!_buffers.TryGetValue(name, out var buffer))
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                buffer.AssignSlot();
                _buffers[name] = buffer;
            }
            return buffer;
        }

        public void UpdateUniforms(UpdateShaderContext ctx, bool updateGlobals)
        {
            ctx.BufferProvider = this;

            if (updateGlobals)
            {
                foreach (var action in Global.Update!.Actions!)
                    action(ctx, Program!);
            }

            foreach (var action in _update!.Actions!)
                action(ctx, Program!);
        }

        public GlProgramGlobal Global { get; }

        public ShaderMaterial Material { get; }

        public GlBaseProgram? Program { get; set; }

    }
}
