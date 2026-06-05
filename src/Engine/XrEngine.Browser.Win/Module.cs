using XrEngine;
using XrEngine.UI.Web;

[assembly: Module(typeof(XrEngine.Browser.Win.Module))]

namespace XrEngine.Browser.Win
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

