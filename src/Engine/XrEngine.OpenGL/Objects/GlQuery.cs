#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public interface IGlQuery : IGlObject
    {
        void Begin(QueryTarget target);

        void End();

        bool IsCompleted();
    }

    public class GlQuery<T> : GlObject, IGlQuery where T : unmanaged
    {
#if GLES
        static ExtDisjointTimerQuery? _timerQuery;
#endif
        private QueryTarget _target;
        private bool _resultFetch;
        private T _lastResult;

        public GlQuery(GL gl)
            : base(gl)
        {
#if GLES
            if (_timerQuery == null)
            {
                gl.TryGetExtension(out _timerQuery);

                if (_timerQuery == null)
                    throw new NotSupportedException();
            }

#endif
            Create();
        }

        public void Begin(QueryTarget target)
        {
            _target = target;
            _resultFetch = false;

            _gl.BeginQuery(target, _handle);
        }

        public void End()
        {

            _gl.EndQuery(_target);
        }

        public bool IsCompleted()
        {
            //out uint: restore after clanup:
            _gl.GetQueryObject(_handle, QueryObjectParameterName.ResultAvailable, out uint available);

            return available != 0;
        }

        public T GetResult()
        {
            if (!_resultFetch)
            {

                if (typeof(T) == typeof(uint))
                {            
                    //out uint: restore after clanup:
                    _gl.GetQueryObject(_handle, QueryObjectParameterName.Result, out uint uintRes);
                    _lastResult = (T)(object)uintRes;
                }

                else if (typeof(T) == typeof(ulong))
                {
#if GLES
                    _timerQuery!.GetQueryObject(_handle, EXT.QueryResultExt, out ulong longRes);
#else
                    _gl.GetQueryObject(_handle, QueryObjectParameterName.Result, out ulong longRes);
#endif

                    _lastResult = (T)(object)longRes;
                }
                else
                    throw new NotSupportedException();

                _resultFetch = true;
            }

            return _lastResult;
        }

        public void Counter()
        {
#if GLES
            _timerQuery!.QueryCounter(_handle, EXT.TimestampExt);
#else
            _gl.QueryCounter(_handle, QueryCounterTarget.Timestamp);
#endif
        }

        public override void Dispose()
        {
            if (_handle != 0)
                _gl.DeleteQuery(_handle);

            base.Dispose();
        }

        protected void Create()
        {
            _handle = _gl.GenQuery();
        }
    }
}
