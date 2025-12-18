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

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Reference), Reference);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Reference = container.Read<Object3D>(nameof(Reference));
        }

        public override void Dispose()
        {
            Reference = null;
            base.Dispose();
        }

        public Object3D? Reference { get; set; }
    }
}
