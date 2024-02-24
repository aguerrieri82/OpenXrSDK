using System.Windows;

namespace Xr.Editor
{
    public class App : Application
    {
        public App()
        {
            var res = (ResourceDictionary)LoadComponent(new Uri("/Xr.Editor.App;component/Resources.xaml", UriKind.Relative));
            Resources = res;
        }

        [STAThread]
        public static void Main()
        {
            var app = new App();


            var window = new Window();
            window.Title = "Xr Editor";

            window.Content = new MainView(new RenderHost());

            app.Run(window);
        }
    }
}
