using OpenXr.Framework;
using OpenXr.Framework.Android;
using System.Reflection;
using XrEngine.Services;


namespace XrEngine.OpenXr.Android
{
    public abstract class XrEngineActivity : XrActivity
    {
        protected XrEngineApp? _engine;

        protected abstract void Build(XrEngineAppBuilder builder);

        protected void Preload(Assembly entry)
        {
            var references = new HashSet<string>();

            void Visit(Assembly assembly)
            {
                global::Android.Util.Log.Debug(nameof(XrEngineActivity), $"Processing assembly {assembly.GetName().Name}");

                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (!references.Add(reference.FullName))
                        continue;

                    global::Android.Util.Log.Debug(nameof(XrEngineActivity), $"Loading ref {reference.Name}");

                    try
                    {
                        var refAssembly = AppDomain.CurrentDomain.Load(reference);

                        Visit(refAssembly);
                    }
                    catch
                    {
                        global::Android.Util.Log.Error(nameof(XrEngineActivity), $"Loading ref {reference.Name} failed");
                    }
                }
            }

            global::Android.Util.Log.Debug(nameof(XrEngineActivity), $"Start preload");

            Visit(entry);

            global::Android.Util.Log.Debug(nameof(XrEngineActivity), $"End preload");
        }


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
