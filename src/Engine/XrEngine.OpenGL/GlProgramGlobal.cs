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
        protected IShaderHandler?[] _lastGlobalHandler = [];
        protected ShaderUpdate? _shaderUpdate;
        public GlProgramGlobal(GL gl, Shader shader)
        {
            Shader = shader;
            _gl = gl;
        }

        public void UpdateProgram(UpdateShaderContext ctx, params IShaderHandler?[] globalHandlers)
        {
            ctx.BufferProvider = this;
            ctx.LastGlobalUpdate = _shaderUpdate;

            var handlersChanged = !_lastGlobalHandler.SequenceEqual(globalHandlers);

            if (_shaderUpdate == null || handlersChanged)
            {
                _lastGlobalHandler = globalHandlers;

                _handlers = [];

                if (Shader is IShaderHandler shaderHandler)
                    _handlers.Add(shaderHandler);

                foreach (var handler in globalHandlers.Where(a => a != null))
                    _handlers.Add(handler!);
            }

            var needUpdate = _shaderUpdate == null || handlersChanged || _handlers.Any(a => a.NeedUpdateShader(ctx));

            if (needUpdate)
            {
                var globalBuilder = new ShaderUpdateBuilder(ctx);

                foreach (var handler in _handlers)
                    handler.UpdateShader(globalBuilder);

                _shaderUpdate = globalBuilder.Result;
                _shaderUpdate.LightsHash = ctx.LightsHash;
                _shaderUpdate.ShaderHandlers = globalHandlers;
                _shaderUpdate.ShaderVersion = Shader.Version;

                Version++;
            }

            UpdateBuffers(ctx);
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


        public void UpdateBuffers(UpdateShaderContext ctx)
        {
            if (_shaderUpdate == null)
                return;

            foreach (var action in _shaderUpdate!.BufferUpdates!)
                action(ctx);
        }

        public void UpdateUniforms(UpdateShaderContext ctx, IUniformProvider uniformProvider)
        {

            if (_shaderUpdate == null)
                return;

            foreach (var action in _shaderUpdate.Actions!)
                action(ctx, uniformProvider);
        }

        public void Dispose()
        {
        }

        public ShaderUpdate? ShaderUpdate => _shaderUpdate;

        public Shader Shader { get; }

        public int Version { get; private set; }
    }
}
