using OpenXr.Framework;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using XrEditor.Audio;
using XrEditor.Plot;
using XrEditor.Services;
using XrEngine;
using XrEngine.Media;
using XrEngine.Media.FFmpeg;
using XrEngine.OpenXr;

namespace XrEditor
{
    public partial class App : Application
    {
        private MainView? _main;
        private readonly WpfViewManager _viewManager;
        private readonly MainDispatcher _mainDispatcher;

        public App()
        {
            DispatcherUnhandledException += (sender, e) =>
            {
                Log.Warn(sender, e.Exception.Message);
                MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            MsBuildPatcher.PatchVisualStudioLinks();

            Gpu.EnableNvAPi();

            if (!EngineNativeLib.RdcIsAttached())
                NvidiaProfiles.DisableOpenGlThreadedOptimization();

            _viewManager = new WpfViewManager();
            _mainDispatcher = new MainDispatcher();

            XrPlatform.Current = new EditorPlatform(EditorDebug.PersistentPath, EditorDebug.UseEs);

            Context.Implement<PanelManager>();
            Context.Implement<NodeManager>();
            Context.Implement<SelectionManager>();
            Context.Implement<PropertyEditorManager>();
            Context.Implement<IViewManager>(_viewManager);
            Context.Implement<IMainDispatcher>(_mainDispatcher);
            Context.Implement<IAssetStore>(MergedAssetStore.FromLocalPaths(EditorDebug.AssetsPath));
            Context.Implement<IVideoReader>(() => new FFmpegVideoReader());
            Context.Implement<IVideoCodec>(() => new FFmpegCodec());
            Context.Implement<IWindowManager>(() => new WpfWindowManager());
            Context.Implement<IClipboard>(() => new WpfClipboard());
            Context.Implement<IProgressLogger>(new NullProgressLogger());
            Context.Implement<IImageFactory>(new WpfImageFactory());

            ModuleManager.Instance.Init();

            ModuleManager.Ref<PlotPanel>();
            ModuleManager.Ref<LoopEditorPanel>();

            MainWindow = new Window
            {
                Title = "Xr Editor",
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _main = new MainView(EditorDebug.Driver)
            {
                Host = new WpfWindow(MainWindow)
            };
            _main.LoadState();

            MainWindow.Content = _main;

            foreach (var res in _viewManager.Resources)
                Resources.MergedDictionaries.Add(res);

            MainWindow.Style = Resources["CustomWindowStyle"] as Style;
            MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/XrEditor.ico", UriKind.RelativeOrAbsolute));
            MainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _mainDispatcher.IsActive = false;

            _main!.SaveState();

            try
            {
                await Context.Require<PanelManager>().CloseAllAsync()
                    .WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch
            {
            }

            ServiceManager.Instance.Shutdown();

            ModuleManager.Instance.Shutdown();

            Process.GetCurrentProcess().Kill();

            base.OnExit(e);
        }
    }
}
