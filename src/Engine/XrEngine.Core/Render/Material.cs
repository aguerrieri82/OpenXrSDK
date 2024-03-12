namespace XrEngine
{
    public enum AlphaMode
    {
        Unspecified,
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

        public virtual void NotifyChanged()
        {
            _host?.NotifyChanged(new ObjectChange(ObjectChangeType.Render, this));
        }

        public virtual Color Color { get; set; }

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public AlphaMode Alpha { get; set; }

        public string? Name { get; set; }
    }
}
