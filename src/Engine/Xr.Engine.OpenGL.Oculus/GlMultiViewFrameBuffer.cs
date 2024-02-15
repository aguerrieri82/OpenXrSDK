#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using System.Runtime.InteropServices;

namespace OpenXr.Engine.OpenGL.Oculus
{
    public class GlMultiViewFrameBuffer : GlFrameBuffer
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
        protected readonly uint _colorTexId;
        protected readonly TextureTarget _target;
        protected uint _width;
        protected uint _height;
        protected uint _depthTexId;

        public GlMultiViewFrameBuffer(GL gl, uint colorTexId, uint sampleCount = 1)
            : base(gl)
        {

            _handle = _gl.GenFramebuffer();

            _sampleCount = sampleCount;
            _colorTexId = colorTexId;
            //_target = sampleCount > 1 ? TextureTarget.Texture2DMultisampleArray : TextureTarget.Texture2DArray;
            _target = TextureTarget.Texture2DArray;

            UpdateTextureInfo();
            CreateDepth();
            BindFunctions(gl);

        }

        protected void UpdateTextureInfo()
        {
            _gl.BindTexture(_target, _colorTexId);
            _gl.GetTexLevelParameter(_target, 0, GetTextureParameter.TextureWidth, out int w);
            _gl.GetTexLevelParameter(_target, 0, GetTextureParameter.TextureHeight, out int h);
            _width = (uint)w;
            _height = (uint)h;
            _gl.BindTexture(_target, 0);
        }

        protected void CreateDepth()
        {
            _depthTexId = _gl.GenTexture();

            _gl.BindTexture(_target, _depthTexId);

            /*
            if (_sampleCount > 1)
            {
                _gl.TexStorage3DMultisample(
                    _target,
                    _sampleCount,
                    SizedInternalFormat.DepthComponent24,
                    _width,
                    _height,
                    2,
                    true);
            }
            else
            */
            {
                _gl.TexStorage3D(
                    _target,
                    _sampleCount,
                    SizedInternalFormat.DepthComponent24,
                    _width,
                    _height,
                    2);
            }

            _gl.BindTexture(_target, 0);
        }

        static void BindFunctions(GL gl)
        {
            nint addr;

            gl.Context.TryGetProcAddress("glFramebufferTextureMultiviewOVR", out addr);
            FramebufferTextureMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultiviewOVRDelegate>(addr);

            gl.Context.TryGetProcAddress("glFramebufferTextureMultisampleMultiviewOVR", out addr);
            FramebufferTextureMultisampleMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultisampleMultiviewOVRDelegate>(addr);
        }

        public override void Dispose()
        {
            _gl.DeleteTexture(_depthTexId);
            _depthTexId = 0;
            base.Dispose();
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
                    _colorTexId,
                    0,
                    _sampleCount,
                    0, 2);

                FramebufferTextureMultisampleMultiviewOVR(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                    _depthTexId,
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
                    _colorTexId,
                    0, 0, 2);

                FramebufferTextureMultiviewOVR(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                    _depthTexId,
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
                return _colorTexId;
            if (attachment == FramebufferAttachment.DepthAttachment)
                return _depthTexId;
            throw new NotSupportedException();
        }
    }
}
