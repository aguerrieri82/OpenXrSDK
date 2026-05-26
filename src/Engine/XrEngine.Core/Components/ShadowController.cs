namespace XrEngine.Components
{
    public class ShadowController : BaseComponent<Scene3D>
    {
        private IShadowMapProvider? _provider;

        public ShadowController()
        {

        }

        protected override void OnAttach()
        {
            _provider = _host?.App?.Renderer?.Feature<IShadowMapProvider>();

            base.OnAttach();
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
