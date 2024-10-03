#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlProgramGlobal : IBufferProvider
    {
        protected readonly Dictionary<string, IGlBuffer> _buffers = [];
        protected readonly GL _gl;
        protected List<IShaderHandler> _handlers = [];

        public GlProgramGlobal(GL gl, Shader shader)
        {
            Shader = shader;
            _gl = gl;
        }

        public void UpdateProgram(UpdateShaderContext ctx, params IShaderHandler?[] globalHandlers)
        {
            ctx.BufferProvider = this;
            ctx.LastUpdate = Update;

            if (Update == null)
            {
                _handlers = [];

                if (Shader.UpdateHandler != null)
                    _handlers.Add(Shader.UpdateHandler);

                foreach (var handler in globalHandlers.Where(a => a != null))
                    _handlers.Add(handler!);
            }

            var needUpdate = Update == null || _handlers.Any(a => a.NeedUpdateShader(ctx));

            if (needUpdate)
            {
                var globalBuilder = new ShaderUpdateBuilder(ctx);

                foreach (var handler in _handlers)
                    handler.UpdateShader(globalBuilder);

                Update = globalBuilder.Result;
                Update.LightsHash = ctx.LightsHash;
                Update.ShaderHandlers = globalHandlers;

                Version++;
            }

            foreach (var action in Update!.BufferUpdates!)
                action(ctx);
        }

        public IBuffer GetBuffer<T>(string name, bool isGlobal)
        {
            if (!_buffers.TryGetValue(name, out var buffer))
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                _buffers[name] = buffer;
            }
            return buffer;
        }

        public void UpdateUniforms(UpdateShaderContext ctx, IUniformProvider uniformProvider)
        {
            if (Update == null)
                return;

            foreach (var action in Update.Actions!)
                action(ctx, uniformProvider);
        }

        public ShaderUpdate? Update { get; set; }

        public Shader Shader { get; }

        public int Version { get; private set; }
    }
}
