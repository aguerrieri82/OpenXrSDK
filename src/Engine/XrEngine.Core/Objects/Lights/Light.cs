using XrMath;

namespace XrEngine
{
    public abstract class Light : Object3D, IDrawGizmos, ISelectionHandler
    {
        protected int _contentVersion;
        protected bool _isSelected;

        public Light()
        {
            Color = Color.White;
            Intensity = 1f;
            CastShadows = true;
        }

        public void Invalidate()
        {
            _contentVersion++;
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ChangeType.Property, ChangeType.Transform))
                change.Type |= ChangeType.Render;

            base.OnChanged(change);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject(this, TypeMode.SubclassesOrSelf);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this, TypeMode.SubclassesOrSelf);
        }

        public virtual void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {

        }

        public void OnSelected(Object3D obj, bool isSelected)
        {
            _isSelected = isSelected;
        }

        public bool CastShadows { get; set; }

        public Color Specular { get; set; }

        public Color Color { get; set; }

        [Range(0, 10, 0.01f)]
        public float Intensity { get; set; }

        public long ContentVersion => _contentVersion;

        bool IDrawGizmos.IsEnabled => _isSelected;
    }
}
