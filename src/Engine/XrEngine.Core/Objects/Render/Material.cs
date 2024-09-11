namespace XrEngine
{
    public enum AlphaMode
    {
        Opaque,
        Mask,
        Blend
    }


    public abstract class Material : EngineObject, IHosted
    {
        readonly HashSet<EngineObject> _hosts = [];

        public Material()
        {
            Alpha = AlphaMode.Opaque;
            Version = 0;
            IsEnabled = true;
        }

        public void Attach(EngineObject host)
        {
            _hosts.Add(host);
        }

        public void Detach(EngineObject host)
        {
            _hosts.Remove(host);
        }

        protected override void OnChanged(ObjectChange change)
        {
            foreach (var host in _hosts)
                host.NotifyChanged(new ObjectChange(ObjectChangeType.Render, this));

            Version++;
            base.OnChanged(change);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<Material>(this);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<Material>(this);
        }

        public IReadOnlySet<EngineObject> Hosts => _hosts;

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public AlphaMode Alpha { get; set; }

        public bool IsEnabled { get; set; }

        public string? Name { get; set; }
    }
}
