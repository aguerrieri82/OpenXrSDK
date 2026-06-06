namespace XrEngine
{
    public class StaticLayer : OpaqueLayer
    {
        public StaticLayer()
        {
            Name = "Static";
        }

        protected override bool BelongsToLayer(Object3D obj)
        {
            var isStatic = obj.Ancestors().Any(a => (a.Flags & EngineObjectFlags.Static) != 0);
            return isStatic && base.BelongsToLayer(obj);
        }
    }
}
