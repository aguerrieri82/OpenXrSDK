namespace XrEngine
{
    public class OpaqueLayer : BaseAutoLayer<TriangleMesh>
    {
        public OpaqueLayer()
        {
            Name = "Opaque";
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials.
                    OfType<ShaderMaterial>().
                    Any(a => a.Alpha != AlphaMode.Blend);
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
