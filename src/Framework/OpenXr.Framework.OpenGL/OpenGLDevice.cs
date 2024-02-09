using Silk.NET.OpenGL;
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
        private Thread? _mainLoopThread;
        private XrDispatcherThread _dispatcher;


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate bool wglMakeCurrentDelegate(nint hdc, nint hglrc);

        static wglMakeCurrentDelegate? wglMakeCurrent;

        public OpenGLDevice(IView? view = null)
        {
            _view = view;
            _dispatcher = new XrDispatcherThread();
        }


       
        protected void OnViewLoaded()
        {
            _gl = _view.CreateOpenGL();

            if (_view!.GLContext!.TryGetProcAddress("wglMakeCurrent", out var wglMakeCurrentPtr))
            {
                wglMakeCurrent = Marshal.GetDelegateForFunctionPointer<wglMakeCurrentDelegate>(wglMakeCurrentPtr);

                wglMakeCurrent(0, 0);
            }

            lock (_view!)
                Monitor.Pulse(_view);
        }

        [MemberNotNull(nameof(_mainLoopThread))]
        public void Initialize(ulong minVer, ulong maxVer)
        {
            if (_mainLoopThread != null)
                return;

            if (_view == null)
                CreateWindow();

            _view.Load += OnViewLoaded;
            _view.Render += OnRender;

            _mainLoopThread = new Thread(_view.Run);
            _mainLoopThread.Name = "OpenGL Main";
            _mainLoopThread.Start();

            lock (_view)
                Monitor.Wait(_view);
        }

        protected virtual void OnRender(double time)
        {
            _dispatcher.ProcessQueue();
        }


        [MemberNotNull(nameof(_view))]
        protected void CreateWindow()
        {
            var options = WindowOptions.Default;

            _view = Window.Create(options);
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.Close();

                if (_mainLoopThread != null)
                {
                    _mainLoopThread.Join();
                    _mainLoopThread = null;
                }

                _view.Dispose();
                _view = null;
            }
        }

        public IView View => _view ?? throw new ArgumentNullException("Not initialized");

        public GL Gl => _gl ?? throw new ArgumentNullException("Not initialized");

        public IXrThread MainThread => _dispatcher;
    }
}
