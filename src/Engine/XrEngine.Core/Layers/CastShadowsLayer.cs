using XrMath;

namespace XrEngine
{
    public class CastShadowsLayer : BaseAutoLayer<TriangleMesh>
    {
        protected Bounds3 _bounds;
        protected long _contentVersion;

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials != null &&
                  obj.Materials.Any(m => m.IsEnabled && m.CastShadows);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.Target is TriangleMesh obj && Contains(obj))
            {
                var builder = new BoundsBuilder();
                builder.Add(_content.Select(a => a.WorldBounds));
                _bounds = builder.Result;
                _contentVersion++;
            }
            return base.AffectChange(change);
        }

        public long ContentVersion => _contentVersion;

        public Bounds3 WorldBounds => _bounds;
    }
}
