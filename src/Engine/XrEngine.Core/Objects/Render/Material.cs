namespace XrEngine
{
    public enum AlphaMode
    {
        Opaque,
        Mask,
        Blend
    }


    public abstract class Material : EngineObject
    {
        protected Object3D? _host;

        public Material()
        {
            Alpha = AlphaMode.Opaque;
            Version = 0;
        }

        //TODO same material multiple objects, wrong
        public void Attach(Object3D host)
        {
            _host = host;
        }

        public void Detach()
        {
            _host = null;
        }

        protected override void OnChanged(ObjectChange change)
        {
            _host?.NotifyChanged(new ObjectChange(ObjectChangeType.Render, this));
            Version++;
            base.OnChanged(change);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            WriteDepth = container.Read<bool>(nameof(WriteDepth));
            UseDepth = container.Read<bool>(nameof(UseDepth));
            WriteColor = container.Read<bool>(nameof(WriteColor));
            WriteColor = container.Read<bool>(nameof(WriteColor));
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(WriteDepth), WriteDepth);
            container.Write(nameof(UseDepth), UseDepth);
            container.Write(nameof(WriteColor), WriteColor);
            container.Write(nameof(DoubleSided), DoubleSided);
            container.Write(nameof(Alpha), Alpha);
            container.Write(nameof(Name), Name);
        }

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public AlphaMode Alpha { get; set; }

        public string? Name { get; set; }
    }
}
