using XrEngine;
using XrEngine.OpenXr.Android;
using XrEngine.UI.Web;

[assembly: Module(typeof(XrEngine.OpenXr.Android.Module))]

namespace XrEngine.OpenXr.Android
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<IWebBrowserFactory>(() => new AndroidWebBrowserFactory());
        }

        public void Shutdown()
        {

        }
    }
}

