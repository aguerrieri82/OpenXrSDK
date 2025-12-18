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
                        Any(a => a.Alpha == AlphaMode.Blend || a.Alpha == AlphaMode.Mask && a is not IVolumeMaterial);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Scene))
                return true;

            if (change.IsAny(ObjectChangeType.Material))
            {
                _version++;
                return true;
            }

            return false;
        }

    }

}
