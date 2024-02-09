using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using Silk.NET.Windowing;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Monitor = System.Threading.Monitor;
using Thread = System.Threading.Thread;

namespace OpenXr.Framework.OpenGL
{
    public class OpenGLDevice : IOpenGLDevice, IDisposable
    {
        private IView? _view;
        private GL? _gl;

        public OpenGLDevice(IView? view = null)
        {
            _view = view;
        }

        public void Initialize(ulong minVer, ulong maxVer)
        {
            if (_view == null)
                CreateWindow();

            _view.Initialize();
            _view.DoEvents();

            _gl = _view.CreateOpenGL();
        }


        [MemberNotNull(nameof(_view))]
        protected void CreateWindow()
        {
            var options = WindowOptions.Default;
            options.IsVisible = false;

            _view = Window.Create(options);
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        public IView View => _view ?? throw new ArgumentNullException("Not initialized");

        public GL Gl => _gl ?? throw new ArgumentNullException("Not initialized");
    }
}
