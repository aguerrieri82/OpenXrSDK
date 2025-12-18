using UI.Binding;
using XrEngine;
using INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged;

namespace XrEditor.Nodes
{
    public class ComponentNode<T> : BaseNode<T>, IEditorProperties, IItemView, IDisposable, INodeChanged where T : IComponent
    {
        protected bool _autoGenProps;

        public ComponentNode(T value) : base(value)
        {
            _autoGenProps = true;
            if (value is INotifyPropertyChanged notify)
                notify.PropertyChanged += OnPropertyChanged;
        }

        protected virtual void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NodeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<T>(_value);
            EditorProperties(binder, curProps);
        }

        protected virtual void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {

        }

        public void Dispose()
        {
            if (_value is INotifyPropertyChanged notify)
                notify.PropertyChanged -= OnPropertyChanged;
        }

        public virtual string DisplayName => _value.GetType().Name;

        public IconView? Icon => null;


        public event EventHandler? NodeChanged;


        bool IEditorProperties.AutoGenerate
        {
            get => _autoGenProps;
            set => _autoGenProps = value;
        }

    }
}
