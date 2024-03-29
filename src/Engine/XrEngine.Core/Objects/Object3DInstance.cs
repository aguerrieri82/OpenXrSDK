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

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.WriteRef(nameof(Reference), Reference);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Reference = container.Read<Object3D>(nameof(Reference));
        }

        public Object3D? Reference { get; set; }
    }
}
