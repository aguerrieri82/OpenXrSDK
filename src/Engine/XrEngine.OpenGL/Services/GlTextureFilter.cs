#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;


namespace XrEngine.OpenGL
{
    public class GlTextureFilter : ITextureFilterProvider
    {
        protected readonly Dictionary<string, GlComputeProgram> _computePrograms = [];
        protected readonly GL _gl;
        protected readonly GlState _glState;

        public GlTextureFilter(OpenGLRender render)
        {
            _gl = render.GL;
            _glState = render.State;
            Context.Implement<ITextureFilterProvider>(this);
            Instance = this;
        }

        public void Dispose()
        {

            foreach (var program in _computePrograms)
                program.Value.Dispose();

            _computePrograms.Clear();

        }

        public void Kernel3x3(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels, int mipLevel)
        {
            var isInit = false;

            if (!_computePrograms.TryGetValue(key, out var program))
            {
                program = new GlComputeProgram(_gl, "Image/Kernel3x3.comp", str => Embedded.GetString<Material>(str));

                if (src.Format == TextureFormat.Rgba32 || src.Format == TextureFormat.SRgba32)
                    program.AddFeature("FORMAT rgba8");

                if (src.Depth > 1 || dst.Depth > 1)
                    program.AddFeature("USE_ARRAY");

                program.AddFeature($"MIP_LEVEL {mipLevel}");
                program.AddFeature("CHANNELS " + activeChannels);

                program.Build();

                _computePrograms[key] = program;

                isInit = true;
            }

            var mipWidth = Math.Max(1, dst.Width >> mipLevel);
            var mipHeight = Math.Max(1, dst.Height >> mipLevel);

            var curProgram = _glState.ActiveProgram;

            program.Use();
            program.SetUniform("texelSize", new Vector2(1f / mipWidth, 1f / mipHeight));

            if (isInit)
                program.SetUniform("weights", data);

            var dstGl = dst.ToGlTexture();

            program.LoadTexture(src, 10);

            var format = dstGl.InternalFormat;

            if (format == InternalFormat.Srgb8Alpha8)
                throw new NotSupportedException();

            _gl.BindImageTexture(0, dstGl, mipLevel, dst.Depth > 1, 0, BufferAccessARB.WriteOnly, format);

            var z = src.Depth == 0 ? 1 : src.Depth;

            _gl.DispatchCompute((uint)((mipWidth + 15) / 16), (uint)((mipHeight + 15) / 16), (uint)z);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _glState.SetActiveProgram(curProgram ?? 0);
        }

        void KernelXOrY(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels, string progName, int mipLevel)
        {
            var isInit = false;

            if (!_computePrograms.TryGetValue(key, out var program))
            {
                program = new GlComputeProgram(_gl, progName, str => Embedded.GetString<Material>(str));
                program.Build();

                if (src.Format == TextureFormat.Rgba32 || src.Format == TextureFormat.SRgba32)
                    program.AddFeature("FORMAT rgba8");

                if (src.Depth > 1 || dst.Depth > 1)
                    program.AddFeature("USE_ARRAY");

                program.AddFeature($"MIP_LEVEL {mipLevel}");
                program.AddFeature("CHANNELS " + activeChannels);

                _computePrograms[key] = program;

                isInit = true;
            }

            var curProgram = _glState.ActiveProgram;

            program.Use();

            if (isInit || true)
            {
                program.SetUniform("uWeights", data);
                program.SetUniform("uRadius", data.Length);
            }

            var dstGl = dst.ToGlTexture();
            var srcGL = src.ToGlTexture();

            _gl.BindImageTexture(0, srcGL, 0, false, 0, BufferAccessARB.ReadOnly, srcGL.InternalFormat);

            _gl.BindImageTexture(1, dstGl, 0, false, 0, BufferAccessARB.WriteOnly, dstGl.InternalFormat);

            _gl.DispatchCompute((dst.Width + 15) / 16, (dst.Height + 15) / 16, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _glState.SetActiveProgram(curProgram ?? 0);
        }

        public void KernelX(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels, int mipLevel = 0)
        {
            KernelXOrY(src, dst, data, key, activeChannels, "Image/kernelX.comp", mipLevel);
        }

        public void KernelY(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels, int mipLevel = 0)
        {
            KernelXOrY(src, dst, data, key, activeChannels, "Image/kernelY.comp", mipLevel);
        }


        public static GlTextureFilter? Instance { get; private set; }
    }
}
