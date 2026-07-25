using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenGL
{
    public class GlSharedWorker : IActiveService
    {
        readonly QueueDispatcher _dispatcher;
        readonly GlRenderOptions _options;
        readonly Thread _thread;
        readonly AutoResetEvent _exitEvent;

        bool _isStarted;
        private IGlContext? _sharedCtx;

        public GlSharedWorker()
        {
            _exitEvent = new AutoResetEvent(false); 
            _thread = new Thread(MainLoop);
            _dispatcher = new QueueDispatcher(_thread);
            _options = OpenGLRender.Current!.Options;

            ServiceManager.Instance.Register(this);
        }

        public void Start()
        {
            if (_isStarted)
                return;

            var privider = Context.Require<IGlContextProvider>();

            _sharedCtx = privider.CreateShared();

            _isStarted = true;  

            _thread.Start();
        }

        protected void MainLoop()
        {
            _sharedCtx!.Take();

            OpenGLRender.Current = new OpenGLRender(_sharedCtx.Gl, _options, isDummy: true);

            while (_isStarted)
            {
                var waitRes = WaitHandle.WaitAny([_dispatcher.WorkAvailable, _exitEvent]);

                if (waitRes == 1)
                    break;

                _dispatcher.ProcessQueue();
            }
        }

        public void Dispose()
        {
            if (_isStarted)
            {
                _isStarted = false;
                _exitEvent.Set();
                _thread.Join();
            }

            _sharedCtx?.Dispose();
            _exitEvent.Dispose();

            GC.SuppressFinalize(this);
        }

        public bool IsStarted => _isStarted;

        public QueueDispatcher Dispatcher => _dispatcher;
    }
}
