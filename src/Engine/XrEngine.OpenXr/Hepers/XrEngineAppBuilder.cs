using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public class XrEngineAppBuilder
    {
        readonly XrEngineAppOptions _options = new();
        readonly List<Action<XrEngineApp>> _configurations = [];
        readonly List<Action<IXrActionBuilder>> _inputs = [];
        Type? _inputProfile;
        EngineApp? _app;
        IXrEnginePlatform? _platform;

        public XrEngineAppBuilder()
        {
            _options.Driver = GraphicDriver.OpenGL;
            _options.ResolutionScale = 1;
            _options.SampleCount = 1;
            _options.RenderMode = XrRenderMode.SingleEye;
            _platform = XrPlatform.Current;
        }

        public XrEngineAppBuilder UsePlatform(IXrEnginePlatform platform)
        {
            _platform = platform;
            XrPlatform.Current = _platform;
            return this;
        }


        public XrEngineAppBuilder ConfigureApp(Action<XrEngineApp> configure)
        {
            _configurations.Add(configure);
            return this;
        }


        public XrEngineAppBuilder UseInputs<TProfile>(Action<XrActionsBuilder<TProfile>> builder) where TProfile : IXrBasicInteractionProfile, new()
        {
            if (_inputProfile != null && _inputProfile != typeof(TProfile))
                throw new ArgumentException("Input profile differ");

            _inputProfile = typeof(TProfile);

            _inputs.Add(a => builder((XrActionsBuilder<TProfile>)a));

            return this;
        }

        public XrEngineAppBuilder UseApp(EngineApp app)
        {
            _app = app;
            return this;
        }

        public XrEngineApp Build()
        {
            _platform ??= XrPlatform.Current;

            if (_platform == null)
                throw new ArgumentNullException("Platform not specified");

            var engine = new XrEngineApp(_options, _platform);

            engine.Create(_app ?? new EngineApp());

            IXrActionBuilder? actionBuilder = null;

            foreach (var config in _configurations)
            {
                if (_inputProfile != null && actionBuilder == null)
                {
                    var builderType = typeof(XrActionsBuilder<>).MakeGenericType(_inputProfile);

                    actionBuilder = (IXrActionBuilder)Activator.CreateInstance(builderType, engine.XrApp)!;

                    engine.Inputs = (IXrBasicInteractionProfile)actionBuilder.Result;
                }

                config(engine);

                while (_inputs.Count > 0)
                {
                    foreach (var input in _inputs)
                        input(actionBuilder!);

                    _inputs.Clear();
                }
            }

            if (actionBuilder != null)
                engine.XrApp.AddActions(actionBuilder);

            engine.XrApp.BindEngineApp(engine.App); //TODO previous was in XrEngineApp.Create, but leads some error

            return engine;
        }

        public XrEngineAppOptions Options => _options;
    }
}
