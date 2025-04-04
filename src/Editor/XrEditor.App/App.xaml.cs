using OpenXr.Framework;
using System.Windows;
using System.Windows.Media.Imaging;
using XrEditor.Audio;
using XrEditor.Plot;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Video;

namespace XrEditor
{
    public partial class App : Application
    {
        private readonly MainView _main;
        private readonly WpfViewManager _viewManager;

        public App()
        {
            Gpu.EnableNvAPi();

            _viewManager = new WpfViewManager();

            XrPlatform.Current = new EditorPlatform("d:\\Projects\\XrEditor");

            Context.Implement<PanelManager>();
            Context.Implement<NodeManager>();
            Context.Implement<SelectionManager>();
            Context.Implement<PropertyEditorManager>();
            Context.Implement<IViewManager>(_viewManager);
            Context.Implement<IMainDispatcher>(new MainDispatcher());
            Context.Implement<IAssetStore>(MergedAssetStore.FromLocalPaths(EditorDebug.AssetsPath));
            Context.Implement<IVideoReader>(() => new FFmpegVideoReader());
            Context.Implement<IVideoCodec>(() => new FFmpegCodec());
            Context.Implement<IWindowManager>(() => new WpfWindowManager());
            Context.Implement<IClipboard>(() => new WpfClipboard());
            Context.Implement<IProgressLogger>(new NullProgressLogger());

            ModuleManager.Instance.Init();

            ModuleManager.Ref<PlotPanel>();
            ModuleManager.Ref<LoopEditorPanel>();

            _main = new MainView(EditorDebug.Driver);
            ImageLight.UseCache = EditorDebug.Driver == GraphicDriver.OpenGL;

            MainWindow = new Window
            {
                Title = "Xr Editor",
                Content = _main
            };

            _main.Host = new WpfWindow(MainWindow);
            _main.LoadState();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (var res in _viewManager.Resources)
                Resources.MergedDictionaries.Add(res);

            MainWindow.Style = Resources["CustomWindowStyle"] as Style;
            MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/XrEditor.ico", UriKind.RelativeOrAbsolute));
            MainWindow.Show();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _main.SaveState();

            _ = Context.Require<PanelManager>().CloseAllAsync();

            ModuleManager.Instance.Shutdown();

            base.OnExit(e);
        }
    }
}
