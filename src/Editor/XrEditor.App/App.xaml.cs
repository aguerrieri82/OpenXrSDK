using OpenXr.Framework;
using System.Windows;
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

            XrPlatform.Current = new EditorPlatform();

            Context.Implement<PanelManager>();
            Context.Implement<NodeManager>();
            Context.Implement<SelectionManager>();
            Context.Implement<PropertyEditorManager>();
            Context.Implement<IMainDispatcher>(new MainDispatcher());
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets"));
            Context.Implement(XrPlatform.Current);
            Context.Implement<IVideoReader>(() => new FFmpegVideoReader());
            Context.Implement<IVideoCodec>(() => new FFmpegCodec());

            ModuleManager.Instance.Init();

            MainWindow = new Window
            {
                Title = "Xr Editor",
                Content = new MainView(GraphicDriver.OpenGL)
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
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
