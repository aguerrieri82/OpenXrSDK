using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.Windowing;
using System.Diagnostics.CodeAnalysis;


namespace OpenXr.Framework.OpenGLES
{
    public class ViewManager
    {
        private IView? _view;
        private GL? _gl;

        public ViewManager(IView? view = null)
        {
            _view = view;
        }

        public void Initialize()
        {
            if (_view == null)
                CreateWindow();

            _view.Initialize();
            _view.DoEvents();

            _gl = _view.CreateOpenGLES();
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
