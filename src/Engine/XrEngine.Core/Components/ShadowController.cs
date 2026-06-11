namespace XrEngine.Components
{
    public class ShadowController : Behavior<Scene3D>
    {
        private IShadowMapProvider? _provider;

        public ShadowController()
        {

        }


        protected override void Start(RenderContext ctx)
        {
            _provider = _host?.App?.Renderer?.Feature<IShadowMapProvider>();

            base.Start(ctx);
        }

        [Action]
        public void Apply()
        {
            foreach (var light in _host!.Children.OfType<DirectionalLight>())
                light.ContentVersion++;
        }

        public ShadowMapOptions? Options
        {
            get => _provider?.Options;
            set => throw new NotSupportedException();
        }
    }
}
