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
        }

        public void Dispose()
        {

            foreach (var program in _computePrograms)
                program.Value.Dispose();

            _computePrograms.Clear();

        }

        public void Kernel3x3(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels)
        {
            var isInit = false;

            if (!_computePrograms.TryGetValue(key, out var program))
            {
                program = new GlComputeProgram(_gl, "Image/Kernel3x3.comp", str => Embedded.GetString<Material>(str));

                if (src.Depth > 0 || dst.Depth > 0)
                    program.AddFeature("USE_ARRAY");

                program.AddFeature("CHANNELS " + activeChannels);

                program.Build();

                _computePrograms[key] = program;

                isInit = true;
            }

            var curProgram = _glState.ActiveProgram;

            program.Use();
            program.SetUniform("texelSize", new Vector2(1f / dst.Width, 1f / dst.Height));

            if (isInit)
                program.SetUniform("weights", data);

            var dstGl = dst.ToGlTexture();

            program.LoadTexture(src, 10);

            _gl.BindImageTexture(0, dst.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, dstGl.InternalFormat);

            var z = src.Depth == 0 ? 1 : src.Depth;

            _gl.DispatchCompute((dst.Width + 15) / 16, (dst.Height + 15) / 16, z);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _glState.SetActiveProgram(curProgram ?? 0);
        }

        void KernelXOrY(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels, string progName)
        {
            var isInit = false;

            if (!_computePrograms.TryGetValue(key, out var program))
            {
                program = new GlComputeProgram(_gl, progName, str => Embedded.GetString<Material>(str));
                program.Build();

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

        public void KernelX(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels)
        {
            KernelXOrY(src, dst, data, key, activeChannels, "Image/kernelX.comp");
        }

        public void KernelY(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels)
        {
            KernelXOrY(src, dst, data, key, activeChannels, "Image/kernelY.comp");
        }


    }
}
