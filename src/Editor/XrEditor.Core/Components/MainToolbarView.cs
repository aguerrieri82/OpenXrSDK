using XrEngine;

namespace XrEditor
{
    public class MainToolbarView : BaseView
    {
        private bool _isMinimized;
        private bool _isHideSelected;

        public MainToolbarView()
        {
            SaveCommand = new Command(SaveAsync);
            LoadCommand = new Command(LoadAsync);
            IsMinimized = true;
        }

        public async Task SaveAsync()
        {
            var container = new JsonStateContainer();
            var scene = EngineApp.Current!.ActiveScene!;

            await EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                scene.GetState(container);
            });

            var json = container.AsJson();
            var fileName = $"scene-{scene.Id}.json";

            if (File.Exists(fileName))
                File.Delete(fileName);

            File.WriteAllText(fileName, container.AsJson());
        }

        public async Task LoadAsync()
        {
            var scene = EngineApp.Current!.ActiveScene!;
            var fileName = $"scene-{scene.Id}.json";

            if (!File.Exists(fileName))
                return;

            var json = File.ReadAllText(fileName);
            var container = new JsonStateContainer(json);
            container.Context.Flags |= StateContextFlags.Update;

            await EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                scene.SetState(container);
            });
        }

        public void HideUnselected(bool value)
        {
            var scene = EngineApp.Current?.ActiveScene;
            if (scene == null)
                return;

            var selLayer = scene.Layers.Layers.First(a => a.Name == "Selection");
            var blendLayer = scene.Layer<BlendLayer>();
            var opaqueLayer = scene.Layer<OpaqueLayer>();
            selLayer.IsVisible = value;
            blendLayer.IsVisible = !value;
            opaqueLayer.IsVisible = !value;
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

        public bool IsHideSelected
        {
            get => _isHideSelected;
            set
            {
                if (_isHideSelected == value)
                    return;
                _isHideSelected = value;
                HideUnselected(value);
                OnPropertyChanged(nameof(IsHideSelected));
            }
        }

        public Command SaveCommand { get; }

        public Command LoadCommand { get; }
    }
}
