namespace XrEngine
{
    public abstract class BaseComponent<T> : IComponent<T> where T : EngineObject
    {
        protected bool _isEnabled;
        protected T? _host;
        protected ObjectId _id;

        public BaseComponent()
        {
            _isEnabled = true;

        }

        protected virtual void OnDisabled()
        {

        }

        protected virtual void OnEnabled()
        {

        }

        protected virtual void OnAttach()
        {

        }


        void IComponent<T>.Attach(T host)
        {
            _host = host;
            OnAttach();
        }

        void IComponent.Detach()
        {
            _host = default;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                if (!_isEnabled)
                    OnDisabled();
                else
                    OnEnabled();
            }
        }


        T? IComponent<T>.Host => _host;

        public ObjectId Id => _id;
    }

}
