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

        bool _isInit;
        private OpenGLRender? _render;

        public AngleGlRenderHost()
        {
            _angleContext = new();
            _glContext = new AngleGlContext(_angleContext);
            Context.Implement<IGlContextProvider>(this);
            Context.Implement(_angleContext);
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

            _render = new OpenGLRender(_glContext.Gl, glOptions);

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
