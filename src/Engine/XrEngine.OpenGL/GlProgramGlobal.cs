#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlProgramGlobal : IBufferProvider, IDisposable
    {
        protected readonly GlBufferMap _bufferMap = new(32);
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
            ctx.LastGlobalUpdate = Update;

            if (Update == null)
            {
                _handlers = [];

                if (Shader is IShaderHandler shaderHandler)
                    _handlers.Add(shaderHandler);

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
                Update.ShaderVersion = Shader.Version;

                Version++;
            }

            foreach (var action in Update!.BufferUpdates!)
                action(ctx);
        }

        public IBuffer<T> GetBuffer<T>(int bufferId, BufferStore store)
        {
            if (store != BufferStore.Shader)
                throw new InvalidOperationException("Invalid buffer store");

            var buffer = (IBuffer<T>?)_bufferMap.Buffers[bufferId];
            if (buffer == null)
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                _bufferMap.Buffers[bufferId] = (IGlBuffer)buffer;
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

        public void Dispose()
        {
        }

        public ShaderUpdate? Update { get; set; }

        public Shader Shader { get; }

        public int Version { get; private set; }
    }
}
