namespace XrEngine
{
    public class BlendLayer : BaseAutoLayer<Object3D>
    {
        public BlendLayer()
        {
            Name = "Blend";
        }

        protected override bool BelongsToLayer(Object3D obj)
        {
            var vertSrc = obj.Feature<IVertexSource>();
            return vertSrc != null &&
                   vertSrc.Materials.
                        OfType<ShaderMaterial>().
                        Any(a => a.Alpha == AlphaMode.Blend || 
                                 a.Alpha == AlphaMode.Punch ||
                                (a.Alpha == AlphaMode.Mask && a is not IVolumeMaterial));
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
