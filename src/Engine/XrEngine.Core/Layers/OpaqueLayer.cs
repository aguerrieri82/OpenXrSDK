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
                        Any(a => (a.Alpha == AlphaMode.Opaque ||
                                  a.Alpha == AlphaMode.BlendMain ||
                                  a.Alpha == AlphaMode.Mask) &&
                                 (a is not IRefractionMaterial refMat || !refMat.HasRefraction));
        }

        protected override void NotifyChangedWork(Object3D sender, ObjectChange change)
        {
            base.NotifyChangedWork(sender, change);

            if (change.IsAny(ChangeType.Material) && Contains(sender))
                OnChanged(sender, Layer3DChangeType.Updated);
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
