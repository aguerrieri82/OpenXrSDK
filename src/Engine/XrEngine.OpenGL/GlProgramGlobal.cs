#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Reflection;

namespace XrEngine.OpenGL
{
    public class GlProgramGlobal : IBufferProvider
    {
        protected readonly Dictionary<string, IGlBuffer> _buffers = [];
        protected readonly GL _gl;
        protected List<IShaderHandler> _handlers = [];

        public GlProgramGlobal(GL gl, Type materialType)
        {
            MaterialType = materialType;
            _gl = gl;
        }

        public void UpdateProgram(UpdateShaderContext ctx, params IShaderHandler?[] globalHandlers)
        {
            ctx.BufferProvider = this;

            if (Update == null)
            {
                _handlers = [];

                var globalProp = MaterialType.GetField("GlobalHandler", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (globalProp != null)
                    _handlers.Add((IShaderHandler)globalProp.GetValue(null)!);

                foreach (var handler in globalHandlers.Where(a=> a != null))
                    _handlers.Add(handler!);
            }

            var needUpdate = Update == null || _handlers.Any(a => a.NeedUpdateShader(ctx, Update));

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

        public Type MaterialType { get; }

        public int Version { get; private set; }
    }
}
