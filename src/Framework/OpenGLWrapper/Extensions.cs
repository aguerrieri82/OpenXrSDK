namespace OpenGLWrapper
{
    public partial interface IGLWrapper
    {
        void BufferData<T>(Action<T> execute, Func<T> getValue);
    }

    public partial class GLWrapper
    {
        public void BufferData<T>(Action<T> execute, Func<T> getValue)
        {
            AddAction(gl =>
            {
                execute(getValue());
            });
        }
    }

    public partial class GLForwardWrapper
    {
        public virtual void BufferData<T>(Action<T> execute, Func<T> getValue)
        {
            _instance.BufferData(execute, getValue);
        }
    }

    public partial class GLDirectWrapper
    {
        public void BufferData<T>(Action<T> execute, Func<T> getValue)
        {
            execute(getValue());
        }
    }
}
