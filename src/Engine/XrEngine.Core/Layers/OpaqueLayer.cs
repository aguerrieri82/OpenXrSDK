namespace XrEngine
{
    public class OpaqueLayer : BaseAutoLayer<Object3D>
    {
        public OpaqueLayer()
        {
            Name = "Opaque";
        }

        protected override bool BelongsToLayer(Object3D obj)
        {
            var vertSrc = obj.Feature<IVertexSource>();
            return vertSrc != null &&
                   vertSrc.Materials.
                        OfType<ShaderMaterial>().
                        Any(a => a.Alpha == AlphaMode.Opaque || a.Alpha == AlphaMode.BlendMain);
        }

        protected override void NotifyChangedWork(Object3D sender, ObjectChange change)
        {
            if (change.IsAny(ChangeType.Material))
                OnChanged(sender, Layer3DChangeType.Updated);

            base.NotifyChangedWork(sender, change);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.IsAny(ChangeType.Scene))
                return true;

            if (change.IsAny(ChangeType.Material))
            {
                _version++;

                return true;
            }

            return false;
        }

    }

}
