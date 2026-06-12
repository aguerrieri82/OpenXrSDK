#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenGLWrapper
{
    public class GlSwitchWrapper : GLForwardWrapper
    {
        readonly GLWrapper _enqueue;
        readonly GLDirectWrapper _direct;

        public GlSwitchWrapper(GL gl)
            : base(new GLDirectWrapper(gl))
        {
            _enqueue = new GLWrapper(gl);
            _direct = (GLDirectWrapper)_instance;

        }

        public void BeginRecord()
        {
            _enqueue.Actions.Clear();
            _instance = _enqueue;
        }

        public List<Action<GL>> EndRecord()
        {
            _instance = _direct;
            return _enqueue.Actions;
        }

        public GLWrapper Enqueue => _enqueue;
    }
}
