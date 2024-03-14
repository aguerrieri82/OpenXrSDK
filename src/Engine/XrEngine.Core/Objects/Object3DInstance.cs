using XrMath;

namespace XrEngine
{
    public class Object3DInstance : Object3D
    {
        public override void UpdateBounds(bool force = false)
        {
            if (Reference == null)
                return;

            _worldBounds = Reference.WorldBounds
                .Transform(Reference.WorldMatrixInverse)
                .Transform(WorldMatrix);

            base.UpdateBounds();
        }

        public override T? Feature<T>() where T : class
        {
            var result = base.Feature<T>();
            if (result != null)
                return result;
            return Reference?.Feature<T>();
        }

        public Object3D? Reference { get; set; }
    }
}
