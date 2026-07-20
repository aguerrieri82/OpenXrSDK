#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Silk.NET.OpenXR;
using Silk.NET.Windowing;

#pragma warning disable CA1422

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

#if !GLES
#if GL_WRAPPER
                    _gl = new OpenGLWrapper.GlSwitchWrapper(_view.CreateOpenGL());

<<<<<<< TODO: Unmerged change from project 'OpenXr.Framework.OpenGL (net11.0-android)', Before:

<<<<<<< TODO: Unmerged change from project 'OpenXr.Framework.OpenGL (net11.0-android)', Before:

<<<<<<< TODO: Unmerged change from project 'OpenXr.Framework.OpenGL (net11.0-android)', Before:

<<<<<<< TODO: Unmerged change from project 'OpenXr.Framework.OpenGL (net11.0-android)', Before:
    #else
                    _gl = _view.CreateOpenGL();
    #endif
=======
=======
#else
                    _gl = _view.CreateOpenGL();
#endif
=======
>>>>>>> After
#else
            _gl = _view.CreateOpenGL();
#endif
=======
=======
#else
            _gl = _view.CreateOpenGL();
#endif
=======
>>>>>>> After
#else
            _gl = _view.CreateOpenGL();
#endif
=======
=======
#else
            _gl = _view.CreateOpenGL();
#endif
=======
>>>>>>> After
#else
            _gl = _view.CreateOpenGL();
#endif
=======
#else
            _gl = _view.CreateOpenGL();
#endif
>>>>>>> After
#else
            _gl = _view.CreateOpenGL();
#endif

#else
            _gl = _view.CreateOpenGLES();
#endif
        }

        public nint HDc => _hdc;

        public nint GlCtx => _glctx;

        public GL Gl => _gl;

    }
}
