using OpenXr.Framework;
using OpenXr.Framework.Android;


namespace XrEngine.OpenXr.Android
{
    public abstract class XrEngineActivity : XrActivity
    {
        protected XrEngineApp? _engine;

        protected abstract void Build(XrEngineAppBuilder builder);

        protected override XrApp CreateApp()
        {

            var builder = new XrEngineAppBuilder()
                   .UsePlatform(new AndroidPlatform(this));

            Build(builder);

            _engine = builder.Build();

            return _engine.XrApp;
        }
    }
}
