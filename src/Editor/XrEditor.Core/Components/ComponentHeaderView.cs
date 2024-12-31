using XrEngine;

namespace XrEditor
{
    public class ComponentHeaderView : BaseView
    {
        readonly IComponent _component;

        public ComponentHeaderView(IComponent component)
        {
            _component = component;
            RemoveCommand = new Command(RemoveAsync);
        }


        public async Task RemoveAsync()
        {
            await EngineApp.Current!.Dispatcher.ExecuteAsync(() =>
            {
                _component.Host?.RemoveComponent(_component);
            });

            OnRemove?.Invoke();
        }

        public bool IsEnabled
        {
            get => _component.IsEnabled;
            set
            {
                _component.IsEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string? Name { get; set; }

        public IconView? Icon { get; set; }

        public Command RemoveCommand { get; }

        public Action? OnRemove { get; set; }
    }
}
