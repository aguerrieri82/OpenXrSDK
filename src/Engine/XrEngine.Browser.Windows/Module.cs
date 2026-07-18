using XrEngine;
using XrEngine.UI.Web;

[assembly: Module(typeof(XrEngine.Browser.Windows.Module))]

namespace XrEngine.Browser.Windows
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<IWebBrowserFactory>(() => new WinWebBrowserFactory());
        }

        public void Shutdown()
        {

        }
    }
}

