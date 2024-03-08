using OpenXr.Framework;
using System.Windows;

namespace XrEditor
{
    public class App : Application
    {
        public App()
        {
            var res = (ResourceDictionary)LoadComponent(new Uri("/XrEditor.App;component/Resources.xaml", UriKind.Relative));
            Resources = res;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ = Context.Require<PanelManager>().CloseAllAsync();

            base.OnExit(e);
        }

        [STAThread]
        public static void Main()
        {
            Gpu.EnableNvAPi();

            Context.Implement<PanelManager>();
            Context.Implement<NodeFactory>();
            Context.Implement<IMainDispatcher>(new MainDispatcher());

            var app = new App();

            var window = new Window
            {
                Title = "Xr Editor",
                Content = new MainView()
            };


            app.Run(window);
        }
    }
}
