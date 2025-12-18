using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class EngineObjectNode<T> : BaseNode<T>, INodeChanged, IItemView, IItemActions, IEditorProperties where T : EngineObject
    {
        protected bool _autoGenProps;
        protected bool _keepChangeListener;

        protected event EventHandler? _nodeChanged;

        public EngineObjectNode(T value)
            : base(value)
        {

        }

        public virtual string DisplayName => _value.GetType().Name;

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<T>(_value);
            EditorProperties(binder, curProps);
        }

        protected virtual void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
        }

        protected virtual void OnObjectChanged(EngineObject obj, ObjectChange change)
        {
            OnNodeChanged();
        }

        protected virtual void OnNodeChanged()
        {
            if (_nodeChanged == null)
                return;

            Context.Require<IMainDispatcher>().ExecuteAsync(() =>
                _nodeChanged?.Invoke(this, EventArgs.Empty));
        }

        public virtual void Actions(IList<ActionView> result)
        {

        }

        bool IEditorProperties.AutoGenerate
        {
            get => _autoGenProps;
            set => _autoGenProps = value;
        }

        public virtual IconView? Icon => null;

        public event EventHandler NodeChanged
        {
            add
            {
                _nodeChanged += value;
                _value.Changed += OnObjectChanged;
            }

            remove
            {
                _nodeChanged -= value;
                if (_nodeChanged == null && !_keepChangeListener)
                    _value.Changed -= OnObjectChanged;
            }
        }
    }
}
