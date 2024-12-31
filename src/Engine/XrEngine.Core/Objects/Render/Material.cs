namespace XrEngine
{
    public enum AlphaMode
    {
        Opaque = 0,
        Mask = 1,
        Blend = 2,
        BlendMain = 4 | Blend
    }

    public enum StencilFunction
    {
        Never = 0x0200,
        Less = 0x0201,
        Equal = 0x0202,
        LEqual = 0x0203,
        Greater = 0x0204,
        NotEqual = 0x0205,
        GEqual = 0x0206,
        Always = 0x0207
    }

    public abstract class Material : EngineObject, IHosted, IMaterial
    {
        protected readonly HashSet<EngineObject> _hosts = [];
        protected bool _isEnabled;

        public Material()
        {
            Alpha = AlphaMode.Opaque;
            IsEnabled = true;
            StencilFunction = StencilFunction.Always;
        }

        public virtual void Attach(EngineObject host)
        {
            _hosts.Add(host);
        }

        public void Detach(EngineObject host)
        {
            Detach(host, false);
        }

        public void Detach(EngineObject host, bool dispose)
        {
            _hosts.Remove(host);
            if (dispose && _hosts.Count == 0)
                Dispose();
        }

        protected override void OnChanged(ObjectChange change)
        {
            foreach (var host in _hosts)
                host.NotifyChanged(new ObjectChange(change.Type, this));

            base.OnChanged(change);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            container.ReadObject(this);
            NotifyChanged(ObjectChangeType.Render);
            base.SetStateWork(container);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<Material>(this);
        }

        public override void GeneratePath(List<string> parts)
        {
            if (_hosts.Count > 0)
            {
                var host = _hosts.First();
                host.GeneratePath(parts);
                if (host is TriangleMesh mesh)
                {
                    var index = mesh.Materials.IndexOf(this);
                    parts.Add($"Materials[{index}]");
                }
            }

            base.GeneratePath(parts);
        }

        public IReadOnlySet<EngineObject> Hosts => _hosts;

        public bool UseClipDistance { get; set; }

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public bool CastShadows { get; set; }

        public byte? WriteStencil { get; set; }

        public byte? CompareStencilMask { get; set; }

        public StencilFunction StencilFunction { get; set; }

        public AlphaMode Alpha { get; set; }

        public string? Name { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                NotifyChanged(ObjectChangeType.MaterialEnabled);
            }
        }

        public virtual Material Clone()
        {
            return (Material)MemberwiseClone();
        }
    }
}
