namespace XrEngine
{
    public class RefractionLayer : BaseAutoLayer<TriangleMesh>
    {
        public RefractionLayer()
        {
            Name = "Refraction";
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials.
                   OfType<IRefractionMaterial>().
                   Any(a => a.HasRefraction);
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
