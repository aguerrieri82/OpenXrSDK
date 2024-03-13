#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using System.Runtime.InteropServices;

namespace XrEngine.OpenGL.Oculus
{
    public class GlMultiViewFrameBuffer : GlBaseFrameBuffer
    {
        static readonly Dictionary<uint, GlMultiViewRenderTarget> _targets = [];


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void FramebufferTextureMultiviewOVRDelegate(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            uint texture,
            uint level,
            uint baseViewIndex,
            uint numViews
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void FramebufferTextureMultisampleMultiviewOVRDelegate(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            uint texture,
            uint level,
            uint samples,
            uint baseViewIndex,
            uint numViews
        );

        static FramebufferTextureMultiviewOVRDelegate? FramebufferTextureMultiviewOVR;

        static FramebufferTextureMultisampleMultiviewOVRDelegate? FramebufferTextureMultisampleMultiviewOVR;

        protected readonly uint _sampleCount;
        protected readonly uint _colorTex;
        protected readonly uint _depthTex;
        protected readonly TextureTarget _target;

        public GlMultiViewFrameBuffer(GL gl, uint colorTex, uint depthTex, uint sampleCount)
            : base(gl)
        {

            _handle = _gl.GenFramebuffer();
            _colorTex = colorTex;
            _depthTex = depthTex;
            _target = TextureTarget.Texture2DArray;
            _sampleCount = sampleCount;

            BindFunctions(gl);
        }

        static void BindFunctions(GL gl)
        {
            nint addr;

            gl.Context.TryGetProcAddress("glFramebufferTextureMultiviewOVR", out addr);
            FramebufferTextureMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultiviewOVRDelegate>(addr);

            gl.Context.TryGetProcAddress("glFramebufferTextureMultisampleMultiviewOVR", out addr);
            FramebufferTextureMultisampleMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultisampleMultiviewOVRDelegate>(addr);
        }

        public override void BindDraw()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _handle);

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR!(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    _colorTex,
                    0,
                    _sampleCount,
                    0, 2);

                FramebufferTextureMultisampleMultiviewOVR(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                    _depthTex,
                    0,
                    _sampleCount,
                    0, 2);

            }
            else
            {
                if (FramebufferTextureMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultiviewOVR not supported");

                FramebufferTextureMultiviewOVR!(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    _colorTex,
                    0, 0, 2);

                FramebufferTextureMultiviewOVR(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                    _depthTex,
                    0, 0, 2);
            }

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public override uint QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _colorTex;
            
            if (attachment == FramebufferAttachment.DepthAttachment)
                return _depthTex;

            throw new NotSupportedException();
        }

    }
}
