
#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
using ExtClipControl = Silk.NET.OpenGLES.Extensions.EXT.ExtClipControl;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework.Angle;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenGL.Wpf;

namespace XrEditor
{
    public class AngleGlRenderHost : RenderHost, IGlContextProvider
    {
        AngleGlContext _glContext;
        AngleVulkanContext _angleContext;
#if GLES
        ExtClipControl? _clipControl;
#endif
        bool _isInit;
        private OpenGLRender? _render;

        public AngleGlRenderHost()
        {
            _angleContext = new();
            _glContext = new AngleGlContext(_angleContext);
            Context.Implement<IGlContextProvider>(this);
            Context.Implement(_angleContext);
        }

        public override void BeginFrame(long frameNum)
        {
#if GLES
            if (_clipControl == null && !_glContext.Gl.TryGetExtension(out _clipControl))
                throw new NotSupportedException();

            _clipControl!.ClipControl(EXT.LowerLeftExt, EXT.NegativeOneToOneExt);
#endif

        }

        public override void EndFrame()
        {
#if GLES
            _clipControl!.ClipControl(EXT.UpperLeftExt, EXT.NegativeOneToOneExt);
#endif
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var handle = base.BuildWindowCore(hwndParent);

            if (!_isInit)
            {
                _angleContext.Initialize([], []);
                _angleContext.CreateWindowSurface(handle.Handle);
                _isInit = true;
            }

            return handle;
        }

        public override IRenderEngine CreateRenderEngine(object? driverOptions)
        {
            var glOptions = driverOptions as GlRenderOptions ?? new GlRenderOptions();

            glOptions.FloatPrecision = ShaderPrecision.High;
            glOptions.Outline.Use = true;

            _render = new OpenGLRender(_glContext.Gl, glOptions, useAngle: true);

            TakeContext();

            return _render;
        }

        public override void EnableVSync(bool enable, int scale = 1)
        {
            _angleContext.SetSwapInterval(scale);
        }

        public override void SwapBuffers()
        {
            _angleContext.SwapBuffers();
        }
        public override void ReleaseContext()
        {
            _angleContext.ReleaseCurrent();
        }

        public override bool TakeContext()
        {
            _angleContext.MakeCurrent();
            return true;
        }

        public IGlContext CreateShared()
        {
            throw new NotImplementedException();
        }

        public IGlContext? Current => _glContext;

        public override bool SupportsDualRender => false;

        public AngleVulkanContext AngleContext => _angleContext;


    }
}
