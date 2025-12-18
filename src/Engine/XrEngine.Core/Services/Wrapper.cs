namespace XrEngine
{
    public abstract class Wrapper<T>
    {
        protected List<Action> _actions = [];
        protected readonly T _instance;

        public Wrapper(T instance)
        {
            _instance = instance;
        }

        public T AddAction(Func<T> func)
        {
            _actions.Add(() => func());
            return func();
        }

        public void AddAction(Action action)
        {
            _actions.Add(action);
        }

        public void Execute()
        {
            foreach (var action in _actions)
                action();
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public List<Action> Actions => _actions;

    }
}
