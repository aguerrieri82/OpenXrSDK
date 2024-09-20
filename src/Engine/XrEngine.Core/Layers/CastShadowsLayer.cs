using System;

namespace XrEngine
{
    public class CastShadowsLayer : BaseAutoLayer<TriangleMesh>
    {
        protected override bool BelongsToLayer(TriangleMesh obj)
        {
           return obj.Materials != null && 
                 obj.Materials.Any(m => m.IsEnabled && m.CastShadows);   
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.Target is TriangleMesh obj && Contains(obj))
                _version++;
            return base.AffectChange(change);
        }
    }
}
