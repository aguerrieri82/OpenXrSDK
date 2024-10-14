using OpenXr.Framework;
using OpenXr.Framework.Android;
using XrEngine.Services;


namespace XrEngine.OpenXr.Android
{
    public abstract class XrEngineActivity : XrActivity
    {
        protected XrEngineApp? _engine;

        protected abstract void Build(XrEngineAppBuilder builder);

        protected override XrApp CreateApp()
        {
            ModuleManager.Instance.Init();

            var builder = new XrEngineAppBuilder()
                   .UsePlatform(new AndroidPlatform(this));

            Build(builder);

            _engine = builder.Build();

            _engine.App.Start();

            return _engine.XrApp;
        }


    }
}
