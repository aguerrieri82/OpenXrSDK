#if GLES

using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Runtime.InteropServices;

namespace XrEngine.OpenGL
{
    public class GlMultiViewFrameBuffer : GlBaseFrameBuffer, IGlFrameBuffer
    {

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
        protected GlTexture? _color;
        protected GlTexture? _depth;
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
            gl.Context.TryGetProcAddress("glFramebufferTextureMultiviewOVR", out nint addr);
            FramebufferTextureMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultiviewOVRDelegate>(addr);

            gl.Context.TryGetProcAddress("glFramebufferTextureMultisampleMultiviewOVR", out addr);
            FramebufferTextureMultisampleMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultisampleMultiviewOVRDelegate>(addr);
        }

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            Configure(GlTexture.Attach(_gl, colorTex, 1, _target), 
                      GlTexture.Attach(_gl, depthTex, 1, _target), 
                      sampleCount);
        }


        public void Configure(GlTexture colorTex, GlTexture depthTex, uint sampleCount)
        {
            _color = colorTex;
            _depth = depthTex;
            _sampleCount = sampleCount;

            var depthAtt = GlUtils.IsDepthStencil(_depth.InternalFormat) ?
                FramebufferAttachment.DepthStencilAttachment :
                FramebufferAttachment.DepthAttachment;

            Bind();

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    Target,
                    FramebufferAttachment.ColorAttachment0,
                    _color,
                    0,
                    _sampleCount,
                    0, 2);


                FramebufferTextureMultisampleMultiviewOVR(
                        Target,
                        depthAtt,
                        _depth,
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
                    _color,
                    0, 0, 2);

                FramebufferTextureMultiviewOVR(
                    Target,
                    depthAtt,
                    _depth,
                    0, 0, 2);
            }

            Check();
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

                _gl.CheckError();
            }

            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public override GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment && _sampleCount > 1)
                return GlDepthUtils.GetDepthUsingFramebufferArray(_gl, this, 2);

            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _color;

            if (attachment == FramebufferAttachment.DepthAttachment)
                return _depth;

            throw new NotSupportedException();
        }

        public GlTexture? Color => _color;

        public IGlRenderAttachment? Depth => _depth;

    }
}
