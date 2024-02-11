namespace OpenXr.Engine
{
    public abstract class Material : EngineObject
    {
        protected Object3D? _host;

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
            _host?.NotifyChanged();
        }

        public Color Color { get; set; }

        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }
    }
}
