﻿namespace XrEngine
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
        readonly HashSet<EngineObject> _hosts = [];
        private bool _isEnabled;

        public Material()
        {
            Alpha = AlphaMode.Opaque;
            IsEnabled = true;
            StencilFunction = StencilFunction.Always;
        }

        public void Attach(EngineObject host)
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
                host.NotifyChanged(new ObjectChange(ObjectChangeType.Material, this));
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
    }
}
