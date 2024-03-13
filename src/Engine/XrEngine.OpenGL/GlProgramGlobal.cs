﻿#if GLES
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
                var globalBuilder = new ShaderUpdateBuilder(ctx);

                var globalProp = MaterialType.GetField("GlobalHandler", BindingFlags.Static | BindingFlags.Public);

                if (globalProp != null)
                    ((IShaderHandler)globalProp.GetValue(null)!).UpdateShader(globalBuilder);

                foreach (var handler in globalHandlers)
                    handler?.UpdateShader(globalBuilder);

                Update = globalBuilder.Result;
            }


            foreach (var action in Update!.BufferUpdates!)
                action(ctx);
        }

        public IBuffer GetBuffer<T>(string name, bool isGlobal)
        {
            if (!_buffers.TryGetValue(name, out var buffer))
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                buffer.AssignSlot();
                _buffers[name] = buffer;
            }
            return buffer;
        }

        public ShaderUpdate? Update { get; set; }

        public Type MaterialType { get; }
    }
}
