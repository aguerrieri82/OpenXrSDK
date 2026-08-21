using System.Numerics;

namespace XrEngine
{
    public enum AlphaMode
    {
        Opaque = 0x1,
        Blend = 0x2,
        Mask = 0x4 | Opaque,
        BlendMain = 0x8 | Blend | Opaque,
        Add = Blend | 0x10,
        Min = Blend | 0x20,
        Max = Blend | 0x40,
        Punch = Blend | 0x80,
        Over = Blend | 0x100,
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

    public enum SkinMode
    {
        Static,
        Dynamic
    }

    public enum MorphMode
    {
        NotEmptyTargets,
        AllTargets,
        DynamicTargets,
    }

    public abstract partial class Material : EngineObject, IHosted, IMaterial
    {
        protected HashSet<EngineObject> _hosts = [];

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
            if (_hosts.Count > 0)
            {
                var changeObj = new ObjectChange(change.Type, this);

                foreach (var host in _hosts)
                    host.NotifyChanged(changeObj);

            }
            base.OnChanged(change);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            container.ReadObject(this);
            NotifyChanged(ChangeType.Render);
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

        [Action]
        public virtual void Reload()
        {
            OnChanged(ChangeType.Render);
        }

        public void NotifyChanged()
        {
            NotifyChanged(ChangeType.Render);
        }

        public override void Invalidate(InvalidateMode mode = InvalidateMode.Content)
        {
            base.Invalidate(mode);
        }

        public override Material Clone(ObjectCloneFlags flags = ObjectCloneFlags.None)
        {
            var newMat = (Material)MemberwiseClone();

            newMat._hosts = [];

            if (newMat._props != null)
                newMat._props = [];

            if (newMat._components != null)
                newMat._components = [];

            CloneWork(newMat, flags);

            return newMat;
        }

        public IReadOnlySet<EngineObject> Hosts => _hosts;

        public bool UseClipDistance { get; set; }

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public bool CullFront { get; set; }

        public bool CastShadows { get; set; }

        public byte? WriteStencil { get; set; }

        public byte? CompareStencilMask { get; set; }

        public Vector2 PolygonOffset { get; set; }

        public StencilFunction StencilFunction { get; set; }

        public AlphaMode Alpha { get; set; }

        public SkinMode Skin { get; set; }

        public MorphMode Morph { get; set; }

        public bool UseSkin { get; set; }

        public bool UseMorph { get; set; }

        public int ShadingRate { get; set; }


        public string? Name { get; set; }

        public int Priority { get; set; }

        [Notify(ChangeType.MaterialEnabled)]
        public partial bool IsEnabled { get; set; }

    }
}
