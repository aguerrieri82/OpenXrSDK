namespace XrEngine
{
    public class OpaqueLayer : BaseAutoLayer<IVertexSource>
    {
        public OpaqueLayer()
        {
            Name = "Opaque";
        }

        protected override bool BelongsToLayer(IVertexSource obj)
        {
            return obj.Materials.
                    OfType<ShaderMaterial>().
                    Any(a => a.Alpha != AlphaMode.Blend && a.Alpha != AlphaMode.Mask);
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
