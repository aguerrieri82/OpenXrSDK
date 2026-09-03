namespace XrEngine
{
    public class TransmissionLayer : BaseAutoLayer<TriangleMesh>
    {
        public TransmissionLayer()
        {
            Name = "Transmission";
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials.
                   OfType<ITransmissionMaterial>().
                   Any(a => a.HasTransmission && (a.TransmissionMode == TransmissionMode.Texture || 
                                                  a.TransmissionMode == TransmissionMode.TextureBackground));
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
