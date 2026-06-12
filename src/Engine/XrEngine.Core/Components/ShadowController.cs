namespace XrEngine.Components
{
    public class ShadowController : Behavior<Scene3D>
    {
        private IShadowMapProvider? _mapProvider;

        private IContactShadowProvider? _contactProvider;

        public ShadowController()
        {

        }


        protected override void Start(RenderContext ctx)
        {
            _mapProvider = _host?.App?.Renderer?.Feature<IShadowMapProvider>();
            _contactProvider = _host?.App?.Renderer?.Feature<IContactShadowProvider>();

            base.Start(ctx);
        }

        [Action]
        public void Apply()
        {
            foreach (var light in _host!.Children.OfType<DirectionalLight>())
                light.ContentVersion++;
        }

        public ShadowMapOptions? MapOptions
        {
            get => _mapProvider?.Options;
            set => throw new NotSupportedException();
        }

        public ContactShadowOptions? ContactOptions
        {
            get => _contactProvider?.Options;
            set => throw new NotSupportedException();
        }
    }
}
