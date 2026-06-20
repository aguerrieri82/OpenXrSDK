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
