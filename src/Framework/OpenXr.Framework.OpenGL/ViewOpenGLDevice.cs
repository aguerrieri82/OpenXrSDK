using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using Silk.NET.Windowing;

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
