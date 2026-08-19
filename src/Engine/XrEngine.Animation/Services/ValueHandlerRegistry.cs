namespace XrEngine.Animation
{
    public class ValueHandlerRegistry
    {
        protected readonly Dictionary<Type, object> _handlers = [];

        public ValueHandlerRegistry()
        {
            Register(new Vector3Handler());
            Register(new QuaternionHandler());
            Register(new FloatHandler());
        }

        public void Register<T>(IAnimationValueHandler<T> handler)
        {
            _handlers[typeof(T)] = handler;
        }

        public IAnimationValueHandler<T> GetHandler<T>()
        {
            if (!_handlers.TryGetValue(typeof(T), out var handler))
                throw new NotSupportedException();

            return (IAnimationValueHandler<T>)handler;
        }
    }
}
