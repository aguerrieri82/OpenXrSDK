using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenXR;

namespace OpenXr.Framework.OpenGL
{
    public class ViewOpenGLDevice : IOpenGLDevice
    {
        readonly IView _view;
        readonly GL _gl;
        readonly nint _hdc;
        readonly nint _glctx;

        public ViewOpenGLDevice(IView view)
        {
            _view = view;
            
            var binding = _view.CreateOpenGLBinding();
            if (binding.Type == StructureType.GraphicsBindingOpenglWin32Khr)
            {
                _hdc = binding.OpenGLWin32Khr.HDC;
                _glctx = binding.OpenGLWin32Khr.HGlrc;
            }
            else
                throw new NotSupportedException();

            _gl = _view.CreateOpenGL();

        }

        public nint HDc => _hdc;

        public nint GlCtx => _glctx;

        public GL Gl => _gl;
    }
}
