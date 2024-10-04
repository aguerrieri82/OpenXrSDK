#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlQuery : GlObject
    {
        private QueryTarget _target;
        private bool _resultFetch;
        private uint _lastResult;

        public unsafe GlQuery(GL gl)
            : base(gl)
        {
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

        public uint GetResult()
        {
            if (!_resultFetch)
            {
                _gl.GetQueryObject(_handle, QueryObjectParameterName.Result, out _lastResult);
                _resultFetch = true;
            }
            return _lastResult;
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                _gl.DeleteQuery(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        protected void Create()
        {
            _handle = _gl.GenQuery();
        }
    }
}
