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

        protected uint _width;
        protected uint _height;
        protected uint _sampleCount;
        protected uint _colorTex;
        protected uint _depthTex;
        protected uint _stencilTex;
        protected readonly TextureTarget _target;


        public GlMultiViewFrameBuffer(GL gl)
            : base(gl)
        {

            _handle = _gl.GenFramebuffer();
            _target = TextureTarget.Texture2DArray;
            _gl.CheckError();

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

        protected void UpdateTextureInfo()
        {
            GlState.Current!.BindTexture(_target, _colorTex);
            _gl.GetTexLevelParameter(_target, 0, GetTextureParameter.TextureWidth, out int w);
            _gl.GetTexLevelParameter(_target, 0, GetTextureParameter.TextureHeight, out int h);
            _width = (uint)w;
            _height = (uint)h;
            GlState.Current!.BindTexture(_target, 0);
        }

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            _colorTex = colorTex;
            _depthTex = depthTex;
            _sampleCount = sampleCount;

            Bind();

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    Target,
                    FramebufferAttachment.ColorAttachment0,
                    _colorTex,
                    0,
                    _sampleCount,
                    0, 2);


                FramebufferTextureMultisampleMultiviewOVR(
                        Target,
                        FramebufferAttachment.DepthStencilAttachment,
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
                    Target,
                    FramebufferAttachment.ColorAttachment0,
                    _colorTex,
                    0, 0, 2);

                FramebufferTextureMultiviewOVR(
                    Target,
                    FramebufferAttachment.DepthStencilAttachment,
                    _depthTex,
                    0, 0, 2);

            }

            _gl.DrawBuffers(GlState.DRAW_COLOR_0);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }

            Unbind();
        }

        public void Detach(FramebufferAttachment attachment)
        {
            Bind();

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    Target,
                    attachment,
                    0,
                    0,
                    _sampleCount,
                    0, 2);

                _gl.CheckError();

            }
            else
            {
                if (FramebufferTextureMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultiviewOVR not supported");

                FramebufferTextureMultiviewOVR(
                    Target,
                    attachment,
                    0,
                    0, 0, 2);
            }

            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }

            Unbind();
        }

        public override GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return GlTexture.Attach(_gl, _colorTex, 0, _target);

            if (attachment == FramebufferAttachment.DepthAttachment)
                return GlTexture.Attach(_gl, _depthTex, 0, _target);

            throw new NotSupportedException();
        }

        public uint ColorTex => _colorTex;

        public uint DepthTex => _depthTex;

    }
}
