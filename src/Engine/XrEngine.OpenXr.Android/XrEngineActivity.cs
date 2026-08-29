using OpenXr.Framework;
using OpenXr.Framework.Android;
using XrEngine.OpenGL;
using XrEngine.Services;
using XrInteraction;

namespace XrEngine.OpenXr.Android
{
    public abstract class XrEngineActivity : XrActivity
    {
        protected XrEngineApp? _engine;

        public XrEngineActivity()
        {

            AppDomain.CurrentDomain.UnhandledException += (sender, arg) =>
            {
                if (arg.ExceptionObject is Exception ex)
                    Log.Error(sender, ex);
            };

            TaskScheduler.UnobservedTaskException += (sender, ex) =>
            {
                Log.Error(sender!, ex.Exception);
            };

            GlDebug.Logger = (object sender, string message, object?[] args) =>
                Log.Debug(sender, message, args);

            Context.Implement<IMainActivity>(this);
        }

        protected abstract void BuildApp(XrEngineAppBuilder builder);

        /*
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
        */

        protected override void ConfigureMainLoop()
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(_engine!.App.Dispatcher));
        }

        protected override XrApp CreateApp()
        {
            ModuleManager.Instance.Init();

            var builder = new XrEngineAppBuilder()
                   .UsePlatform(new AndroidPlatform(this));

            BuildApp(builder);

            _engine = builder.Build();

            _engine.App.Start();

            return _engine.XrApp;
        }
    }
}
