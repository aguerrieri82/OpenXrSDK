﻿namespace XrEngine
{
    public class BlendLayer : BaseAutoLayer<TriangleMesh>
    {
        public BlendLayer()
        {
            Name = "Blend";
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.Materials.
                    OfType<ShaderMaterial>().
                    Any(a => a.Alpha == AlphaMode.Blend);
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Scene))
                return true;

            if (change.IsAny(ObjectChangeType.Material))
                return true;

            return false;
        }

    }

}
