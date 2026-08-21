namespace XrEngine
{
    public class VolumeLayer : BaseAutoLayer<TriangleMesh>
    {
        public VolumeLayer()
        {
            Name = "Volume";
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials.
                    OfType<IVolumeMaterial>().
                    Any();
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
