namespace OpenGLWrapper
{
    public abstract class Wrapper<T>
    {
        protected List<Action<T>> _actions = [];
        protected T _instance;

        public Wrapper(T instance)
        {
            _instance = instance;
        }

        public void AddAction(Action<T> action)
        {
            _actions.Add(action);
        }

        public void Execute()
        {
            foreach (var action in _actions)
                action(_instance);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public T Instance => _instance;

        public List<Action<T>> Actions => _actions;

    }
}
