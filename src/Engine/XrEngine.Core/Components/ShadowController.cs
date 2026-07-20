namespace XrEngine.Components
{
    public class ShadowController : BaseComponent<Scene3D>
    {
        private IShadowMapProvider? _mapProvider;

        private IContactShadowProvider? _contactProvider;

        [Action]
        public void Apply()
        {
            foreach (var light in _host!.Children.OfType<DirectionalLight>())
                light.Invalidate();
        }

        public ShadowMapOptions? MapOptions
        {
            get
            {
                _mapProvider ??= _host?.App?.Renderer.Feature<IShadowMapProvider>();
                return _mapProvider?.Options;
            }
            set => throw new NotSupportedException();
        }

        public ContactShadowOptions? ContactOptions
        {
            get
            {
                _contactProvider ??= _host?.App?.Renderer.Feature<IContactShadowProvider>();
                return _contactProvider?.Options;
            }
            set => throw new NotSupportedException();
        }
    }
}
