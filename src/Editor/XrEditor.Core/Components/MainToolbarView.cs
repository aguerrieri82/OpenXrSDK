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
            JsonStateContainer container = new JsonStateContainer();
            Scene3D scene = EngineApp.Current!.ActiveScene!;

            await EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                scene.GetState(container);
            });

            string json = container.AsJson();
            string fileName = $"scene-{scene.Id}.json";

            if (File.Exists(fileName))
                File.Delete(fileName);

            File.WriteAllText(fileName, container.AsJson());
        }

        public async Task LoadAsync()
        {
            Scene3D scene = EngineApp.Current!.ActiveScene!;
            string fileName = $"scene-{scene.Id}.json";

            if (!File.Exists(fileName))
                return;

            string json = File.ReadAllText(fileName);
            JsonStateContainer container = new JsonStateContainer(json);
            container.Context.Flags |= StateContextFlags.Update;

            await EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                scene.SetState(container);
            });
        }

        public void HideUnselected(bool value)
        {
            Scene3D? scene = EngineApp.Current?.ActiveScene;
            if (scene == null)
                return;

            ILayer3D selLayer = scene.Layers.Layers.First(a => a.Name == "Selection");
            BlendLayer blendLayer = scene.Layer<BlendLayer>();
            OpaqueLayer opaqueLayer = scene.Layer<OpaqueLayer>();
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
