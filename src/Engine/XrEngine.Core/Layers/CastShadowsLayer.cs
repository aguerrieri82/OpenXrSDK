using XrMath;

namespace XrEngine
{
    public class CastShadowsLayer : BaseAutoLayer<TriangleMesh>
    {
        protected Bounds3 _bounds;
        protected long _contentVersion;
        protected bool _boundsDirty;

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials != null &&
                   // obj.IsVisible &&
                   obj.Materials.Any(m => m.IsEnabled && m.CastShadows);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.Targets<TriangleMesh>().Any(Contains))
            {
                _boundsDirty = true;
                _contentVersion++;
            }

            if (change.Type == ObjectChangeType.Transform)
                return false;

            return base.AffectChange(change);
        }

        public long ContentVersion => _contentVersion;

        public Bounds3 WorldBounds
        {
            get
            {
                if (_boundsDirty)
                {
                    var builder = new Bounds3Builder();
                    builder.Add(_content.Select(a => a.WorldBounds));
                    _bounds = builder.Result;
                    _boundsDirty = false;
                }
                return _bounds;
            }
        }
    }
}
