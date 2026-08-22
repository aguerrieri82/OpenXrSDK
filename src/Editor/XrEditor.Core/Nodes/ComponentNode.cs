using UI.Binding;
using XrEngine;
using INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged;

namespace XrEditor.Nodes
{
    public class ComponentNode<T> : BaseNode<T>, IEditorProperties, IItemView, IDisposable, INodeChanged, IPresetManager where T : IComponent
    {
        protected PropertiesGenerationMode _autoGenProps;

        public ComponentNode(T value) : base(value)
        {
            _autoGenProps = PropertiesGenerationMode.None;
            if (value is INotifyPropertyChanged notify)
                notify.PropertyChanged += OnPropertyChanged;
        }

        protected virtual void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NodeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<T>(_value, a => EngineApp.Current.Dispatcher.Post(a));
            EditorProperties(binder, curProps);
        }

        protected virtual void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            var curType = _value.GetType();
            while (true)
            {
                PropertyView.CreateProperties(_value, curType, curProps);

                curType = curType.BaseType;

                if (curType == null ||
                    curType == typeof(object) ||
                    (curType.IsGenericType &&
                        (curType.GetGenericTypeDefinition() == typeof(BaseComponent<>) ||
                         curType.GetGenericTypeDefinition() == typeof(Behavior<>) ||
                         curType.GetGenericTypeDefinition() == typeof(AsyncBehavior<>))))
                {
                    break;
                }
            }

        }

        public void Dispose()
        {
            if (_value is INotifyPropertyChanged notify)
                notify.PropertyChanged -= OnPropertyChanged;
        }

        public virtual string DisplayName => _value.GetType().Name;

        public IconView? Icon => null;

        public event EventHandler? NodeChanged;

        PropertiesGenerationMode IEditorProperties.AutoGenerate
        {
            get => _autoGenProps;
            set => _autoGenProps = value;
        }

    }
}
