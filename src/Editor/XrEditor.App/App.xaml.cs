using OpenXr.Framework;
using System.Windows;
using System.Windows.Media.Imaging;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Services;
using XrEngine.Video;

namespace XrEditor
{
    public partial class App : Application
    {
        public App()
        {
            Gpu.EnableNvAPi();

            Fluent.Ribbon x;
   
            XrPlatform.Current = new EditorPlatform("d:\\Projects\\XrEditor");

            Context.Implement<PanelManager>();
            Context.Implement<NodeManager>();
            Context.Implement<SelectionManager>();
            Context.Implement<PropertyEditorManager>();
            Context.Implement<IMainDispatcher>(new MainDispatcher());
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets")); ;
            Context.Implement<IVideoReader>(() => new FFmpegVideoReader());
            Context.Implement<IVideoCodec>(() => new FFmpegCodec());
            Context.Implement<IPanelManager>(() => new WpfPanelManager());

            ModuleManager.Instance.Init();

            MainWindow = new Window
            {
                Title = "Xr Editor",
                Content = new MainView(EditorDebug.Driver),

            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow.Style = Resources["CustomWindowStyle"] as Style;
            MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/XrEditor.ico", UriKind.RelativeOrAbsolute));
            MainWindow.Show();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ = Context.Require<PanelManager>().CloseAllAsync();

            base.OnExit(e);
        }
    }
}
