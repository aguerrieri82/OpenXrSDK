﻿namespace XrEngine
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

        public override void NotifyChanged(ObjectChange change)
        {
            _host?.NotifyChanged(new ObjectChange(ObjectChangeType.Render, this));
            Version++;
            base.NotifyChanged(change);
        }


        public bool WriteDepth { get; set; }

        public bool UseDepth { get; set; }

        public bool WriteColor { get; set; }

        public bool DoubleSided { get; set; }

        public AlphaMode Alpha { get; set; }

        public string? Name { get; set; }
    }
}
