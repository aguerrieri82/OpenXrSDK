#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlProgramGlobal : IBufferProvider, IDisposable
    {
        public class ContextShaderHandler : IShaderHandler
        {
            private readonly ChangeTracker _tracker = new();


            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                return _tracker.IsChanged(() => ctx.IsSrgbAutoEncode) ||
                       _tracker.IsChanged(() => ctx.IsSrgbTarget) ||
                       _tracker.IsChanged(() => ctx.UseCopyDepth);
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {

                if (bld.Context.IsSrgbAutoEncode)
                    bld.AddFeature("SRGB_AUTO_ENCODE");

                if (bld.Context.IsSrgbTarget)
                    bld.AddFeature("SRGB_TARGET");

                if (bld.Context.NeedSrgbEncode)
                    bld.AddFeature("SRGB_ENCODE");

                if (OpenGLRender.Current!.Options.UseHighQualitySrgb)
                    bld.AddFeature("HIGH_QUALITY_SRGB");

                if (bld.Context.UseCopyDepth)
                    bld.AddFeature("COPY_DEPTH");

                if (bld.Context.CopyDepthImage != null && bld.Context.CopyDepthImage.Tag == null)
                {
                    bld.AddFeature("COPY_DEPTH_IMG");

                    bld.ExecuteAction((ctx, up) =>
                    {
                        Debug.Assert(ctx.CopyDepthImage?.Tag == null);

                        if (ctx.CopyDepthImage == null)
                            return;

                        up.LoadImage(ctx.CopyDepthImage, ImagesSlots.Depth, BufferAccessMode.Write);

                        var size = new Vector2(ctx.CopyDepthImage.Width, ctx.CopyDepthImage.Height);
                        var scale = size / ctx.PassCamera!.ViewSize.ToVector2();

                        up.SetUniform("uDepthImageScale", scale);
                    });

       
                }

            }
        }

        public static int MAX_BUFFERS = 30;

        protected readonly GlBufferArray<IGlBufferRange> _modelBufferRanges;
        protected readonly GlBufferArray<IGlBufferRange> _materialBufferRanges;

        protected readonly GlBufferArray<IGlBuffer> _bufferMap;
        protected readonly GL _gl;
        protected List<IShaderHandler> _handlers = [];
        protected IShaderHandler?[] _lastGlobalHandler = [];
        protected ShaderUpdate? _shaderUpdate;

        protected ContextShaderHandler _contextHandler;


        public GlProgramGlobal(GL gl, Shader shader)
        {
            Shader = shader;
            _gl = gl;
            _modelBufferRanges = new GlBufferArray<IGlBufferRange>(MAX_BUFFERS, this);
            _materialBufferRanges = new GlBufferArray<IGlBufferRange>(MAX_BUFFERS, this);
            _bufferMap = new GlBufferArray<IGlBuffer>(MAX_BUFFERS, this);
            _contextHandler = new();
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

                _handlers.Add(_contextHandler);
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

        public GlBufferRange<T> GetBufferRange<T>(int bufferId, BufferStore store, string uniformName)
        {
            var rangeBuffers = store == BufferStore.Material ? _materialBufferRanges : _modelBufferRanges;

            var range = (GlBufferRange<T>?)rangeBuffers.Buffers[bufferId];

            if (range == null)
            {
                range = new GlBufferRange<T>(_gl, uniformName, bufferId);

                rangeBuffers.Buffers[bufferId] = range;
            }

            return range;
        }

        public ISimpleBuffer<T> GetBuffer<T>(int bufferId, BufferStore store, BufferUsage usage, string? uniformName = null)
        {
            if (store != BufferStore.Shader)
                throw new InvalidOperationException("Invalid buffer store");

            var buffer = (IBuffer<T>?)_bufferMap.Buffers[bufferId];

            if (buffer == null)
            {
                var target = usage == BufferUsage.SSbo ? BufferTargetARB.ShaderStorageBuffer : BufferTargetARB.UniformBuffer;

                buffer = new GlBuffer<T>(_gl, target);

                _bufferMap.Buffers[bufferId] = (IGlBuffer)buffer;
            }
            return buffer;
        }

        public void UpdateBuffers(UpdateShaderContext ctx)
        {
            if (_shaderUpdate?.BufferUpdates == null)
                return;

            foreach (var action in _shaderUpdate.BufferUpdates)
                action(ctx);
        }

        public void UpdateUniforms(UpdateShaderContext ctx, IUniformProvider uniformProvider)
        {
            if (_shaderUpdate == null)
                return;

            foreach (var action in _shaderUpdate.Actions)
                action(ctx, uniformProvider);
        }

        public void Dispose()
        {
            _bufferMap.Dispose();
            _materialBufferRanges.Dispose();
            _modelBufferRanges.Dispose();
            GC.SuppressFinalize(this);
        }

        public ShaderUpdate? ShaderUpdate => _shaderUpdate;

        public Shader Shader { get; }

        public int Version { get; private set; }
    }
}
