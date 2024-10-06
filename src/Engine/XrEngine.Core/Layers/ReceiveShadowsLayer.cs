using XrMath;

namespace XrEngine
{
    public class ReceiveShadowsLayer : BaseAutoLayer<TriangleMesh>
    {
        private Bounds3 _bounds;

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials != null &&
                   obj.Materials.OfType<IShadowMaterial>().Any(m => m.IsEnabled && m.ReceiveShadows);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.Target is TriangleMesh obj && Contains(obj))
            {
                var builder = new BoundsBuilder();
                builder.Add(_content.Select(a => a.WorldBounds));
                _bounds = builder.Result;
                _version++;
            }

            return base.AffectChange(change);
        }


        public Bounds3 WorldBounds => _bounds;
    }
}
