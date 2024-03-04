#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public class GlProgramInstance : IBufferProvider
    {
        static Dictionary<string, GlProgram> _programs = [];

        protected Dictionary<Type, IBuffer> _buffers = [];
        protected ShaderUpdate? _update;
        protected readonly GL _gl;

        public GlProgramInstance(GL gl, ShaderMaterial material, GlProgramGlobal global)
        {
            _gl = gl;
            Material = material;
            Global = global;
        }

        public void UpdateProgram(UpdateShaderContext ctx)
        {
            if (Program != null)
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
                program.BufferProvider = this;

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

                foreach (var action in _update!.BufferUpdates!)
                    action(ctx);
            }

            Program = program;
        }

        public IBuffer GetBuffer<T>(string name, bool isGlobal)
        {
            if (isGlobal)
                return Global.GetBuffer<T>(name, true);

            if (!_buffers.TryGetValue(typeof(T), out var buffer))
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                _buffers[typeof(T)] = buffer; 
            }
            return buffer;
        }


        public void UpdateUniforms(UpdateShaderContext ctx)
        {
            ctx.BufferProvider = Global;

            foreach (var action in Global.Update!.Actions!)
                action(ctx, Program!);

            ctx.BufferProvider = this;

            foreach (var action in _update!.Actions!)
                action(ctx, Program!);
        }

        public GlProgramGlobal Global { get; }

        public ShaderMaterial Material { get; }

        public GlProgram? Program { get; set; }

    }
}
