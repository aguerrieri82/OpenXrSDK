using XrEngine;
using XrEngine.Services;

namespace XrEditor
{
    public class MainToolbarView : BaseView
    {
        private bool _isMinimized;

        public MainToolbarView()
        {
            SaveCommand = new Command(SaveAsync);
            LoadCommand = new Command(LoadAsync);
            IsMinimized = true;
        }

        public Task SaveAsync()
        {
            var container = new JsonStateContainer();
            EngineApp.Current!.ActiveScene!.GetState(container);
            var json = container.AsJson();
            File.WriteAllText("scene.json", container.AsJson());
            return Task.CompletedTask;
        }

        public Task LoadAsync()
        {
            IsMinimized = true;
            return Task.CompletedTask;
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                _isMinimized = value;
                OnPropertyChanged(nameof(IsMinimized));
            }
        }

        public Command SaveCommand { get; }

        public Command LoadCommand { get; }
    }
}
