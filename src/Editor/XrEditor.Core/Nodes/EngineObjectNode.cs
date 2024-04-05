using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class EngineObjectNode<T> : BaseNode<T>, IItemView, IEditorProperties where T : EngineObject
    {
        protected bool _autoGenProps;

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

        bool IEditorProperties.AutoGenerate
        {
            get => _autoGenProps;
            set => _autoGenProps = value;
        }

        public virtual IconView? Icon => null;
    }
}
