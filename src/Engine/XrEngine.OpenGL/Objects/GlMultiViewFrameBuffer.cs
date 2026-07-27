#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XrEngine.OpenGL
{
    public class GlMultiViewFrameBuffer : GlBaseFrameBuffer, IGlFrameBuffer
    {

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void FramebufferTextureMultiviewOVRDelegate(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            uint texture,
            uint level,
            uint baseViewIndex,
            uint numViews
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void FramebufferTextureMultisampleMultiviewOVRDelegate(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            uint texture,
            uint level,
            uint samples,
            uint baseViewIndex,
            uint numViews
        );

        public static FramebufferTextureMultiviewOVRDelegate? FramebufferTextureMultiviewOVR;

        public static FramebufferTextureMultisampleMultiviewOVRDelegate? FramebufferTextureMultisampleMultiviewOVR;

        protected uint _width;
        protected uint _height;
        protected uint _sampleCount;
        protected GlTexture? _color;
        protected GlTexture? _depth;
        protected readonly TextureTarget _target;

        public GlMultiViewFrameBuffer(GL gl)
            : base(gl)
        {
            BaseViewIndex = 0;
            NumViews = 2;

            _handle = _gl.GenFramebuffer();
            _target = TextureTarget.Texture2DArray;

            _gl.CheckError();

            BindFunctions(gl);
        }

        static void BindFunctions(GL gl)
        {
            gl.Context.TryGetProcAddress("glFramebufferTextureMultiviewOVR", out var addr);
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

        public void Configure(GlTexture? colorTex, GlTexture? depthTex, uint sampleCount)
        {
            BeginUpdate();

            _color = colorTex;
            _depth = depthTex;
            _sampleCount = sampleCount;

            if (_color != null)
                Attach(_color, FramebufferAttachment.ColorAttachment0, true);

            if (_depth != null)
            {
                var depthAtt = GlUtils.IsDepthStencil(_depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;

                Attach(_depth, depthAtt, false);
            }

            EndUpdate();
        }

        public override void Attach(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw, int layer = 0)
        {
            Bind();

            if (attachment is not GlTexture glTex)
                throw new NotSupportedException();

            if (_sampleCount > 1 && (attachment is GlTexture tex))
            {
                Debug.Assert(tex.Target == TextureTarget.Texture2DArray);

                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    FramebufferTarget.Framebuffer,
                    slot,
                    glTex,
                    (uint)layer,
                    _sampleCount,
                    BaseViewIndex, NumViews);
            }
            else
            {
                if (FramebufferTextureMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultiviewOVR not supported");

                FramebufferTextureMultiviewOVR!(
                    FramebufferTarget.Framebuffer,
                    slot,
                    glTex,
                    (uint)layer, BaseViewIndex, NumViews);
            }

            _isDirty = true;
        }

        public override void Detach(FramebufferAttachment attachment)
        {
            Bind();

            if (_sampleCount > 1)
            {
                if (FramebufferTextureMultisampleMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultisampleMultiviewOVR not supported");

                FramebufferTextureMultisampleMultiviewOVR(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    0,
                    0,
                    _sampleCount,
                    BaseViewIndex, NumViews);

                _gl.CheckError();

            }
            else
            {
                if (FramebufferTextureMultiviewOVR == null)
                    throw new Exception("glFramebufferTextureMultiviewOVR not supported");

                FramebufferTextureMultiviewOVR(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    0,
                    0, BaseViewIndex, NumViews);

                _gl.CheckError();
            }

            Check();
        }

        public override GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _color;

            if (attachment == FramebufferAttachment.DepthAttachment)
                return _depth;

            throw new NotSupportedException();
        }

        public override GlTexture? Color => _color;

        public override IGlRenderAttachment? Depth => _depth;

        public override uint SampleCount => _sampleCount;

        public uint BaseViewIndex { get; set; }

        public uint NumViews { get; set; }

    }
}
