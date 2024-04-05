namespace XrEngine
{
    public abstract class BaseComponent<T> : IComponent<T>, IStateManager where T : EngineObject
    {
        protected bool _isEnabled;
        protected T? _host;
        protected ObjectId _id;

        public BaseComponent()
        {
            _isEnabled = true;

        }

        public void EnsureId()
        {
            if (_id.Value == 0)
                _id = ObjectId.New();
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

        public virtual void GetState(IStateContainer container)
        {
            if (_id.Value == 0)
                _id = ObjectId.New();

            container.Write(nameof(Id), _id.Value);
            container.Write(nameof(IsEnabled), IsEnabled);
        }

        public void SetState(IStateContainer container)
        {
            SetStateWork(container);
        }

        protected virtual void SetStateWork(IStateContainer container)
        {
            _id.Value = container.Read<uint>(nameof(Id));
            IsEnabled = container.Read<bool>(nameof(IsEnabled));
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
