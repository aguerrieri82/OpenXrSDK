#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Runtime.InteropServices;
using XrMath;

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
        protected Size2I _size;
        protected readonly TextureTarget _target;
        protected readonly Dictionary<FramebufferAttachment, IGlRenderAttachment> _attachments = [];

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

            BindAttachment(_color, FramebufferAttachment.ColorAttachment0, true);

            BindAttachment(_depth, depthAtt, false);

            Check();

            _size = new Size2I(_color.Width, _color.Height);
        }

        public void BindAttachment(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw)
        {
            if (attachment is not GlTexture glTex)
                throw new NotSupportedException();

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    Target,
                    slot,
                    glTex,
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
                    slot,
                    glTex,
                    0, 0, 2);
            }
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

        public GlTexture GetOrCreateEffect(FramebufferAttachment slot)
        {
            if (Color == null)
                throw new NotSupportedException();

            if (!_attachments.TryGetValue(slot, out var obj))
            {
                var glTex = Color.Clone(false);

                Bind();
                BindAttachment(glTex, slot, true);
                SetDrawBuffers(DrawBufferMode.ColorAttachment0, (DrawBufferMode)slot);
                Check();

                obj = glTex;
            }

            return (GlTexture)obj;
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

        public Size2I Size => _size;

        public GlTexture? Color => _color;

        public IGlRenderAttachment? Depth => _depth;

    }
}
