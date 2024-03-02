using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public class GlProgramInstance : IGlBufferProvider
    {
        static Dictionary<Type, IBuffer> _globalBuffers = [];
        static Dictionary<Shader, ShaderUpdate> _globalUpdates = [];
        static Dictionary<string, GlProgram> _programs = [];

        protected Dictionary<Type, IBuffer> _buffers = [];
        protected GL _gl;

        public GlProgramInstance(GL gl, ShaderMaterial material)
        {
            _gl = gl;
            Material = material;
        }

        public void UpdateProgram(UpdateShaderContext updateCtx)
        {
            if (Program != null)
                return;

            if (!_globalUpdates.TryGetValue(Material!.Shader!, out var globalUpdate))
            {
                var globalProp = Material.GetType().GetField("GlobalHandler", BindingFlags.Static | BindingFlags.Public);
                if (globalProp != null)
                {
                    var globalBuilder = new ShaderUpdateBuilder(updateCtx);
                    ((IShaderHandler)globalProp.GetValue(null)!).UpdateShader(globalBuilder);
                    globalUpdate = globalBuilder.Result;
                    _globalUpdates[Material.Shader!] = globalUpdate;
                }
            }

            var localBuilder = new ShaderUpdateBuilder(updateCtx);
            Material!.UpdateShader(localBuilder);
            localBuilder.ComputeHash(Material.Shader!.GetType().FullName!);

            InstanceUpdate = localBuilder.Result;

            if (!_programs.TryGetValue(InstanceUpdate.FeaturesHash!, out var program))
            {
                var shader = Material.Shader!;
                var resolver = shader.Resolver!;

                program = new GlSimpleProgram(_gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver);
                program.BufferProvider = this;

                foreach (var ext in InstanceUpdate.Extensions!)
                    program.AddExtension(ext);

                foreach (var feature in InstanceUpdate.Features!)
                    program.AddFeature(feature);

                if (globalUpdate != null)
                {
                    foreach (var ext in globalUpdate.Extensions!)
                        program.AddExtension(ext);

                    foreach (var feature in globalUpdate.Features!)
                        program.AddFeature(feature);
                }

                program.Build();

                _programs[InstanceUpdate.FeaturesHash!] = program;
            }

            Program = program;
        }

        public GlBuffer<T> GetBuffer<T>(string name, bool isGlobal)
        {
            var dictionary = isGlobal ? _globalBuffers : _buffers;  

            if (!dictionary.TryGetValue(typeof(T), out var buffer))
            {
                buffer = new GlBuffer<T>(_gl, BufferTargetARB.UniformBuffer);
                dictionary[typeof(T)] = buffer; 
            }
            return (GlBuffer<T>)buffer;
        }

        public void UpdateGlobal(UpdateShaderContext ctx)
        {
            if (_globalUpdates.TryGetValue(Material!.Shader!, out var update))
            {
                foreach (var action in update.BufferUpdates!)
                    action(ctx, Program!);
            }
        }

        public void UpdateInstance(UpdateShaderContext ctx)
        {
            if (_globalUpdates.TryGetValue(Material!.Shader!, out var globalUpdate))
            {
                foreach (var action in globalUpdate.Actions!)
                    action(ctx, Program!);
            }

            foreach (var action in InstanceUpdate!.BufferUpdates!)
                action(ctx, Program!);

            foreach (var action in InstanceUpdate!.Actions!)
                action(ctx, Program!);
        }

        public ShaderUpdate? InstanceUpdate { get; set; }

        public GlProgram? Program { get; set; }

        public ShaderMaterial? Material { get; set; }   
    }
}
