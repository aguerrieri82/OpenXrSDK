using System.Numerics;

namespace XrEngine
{
    public class DirectionalLight : Light
    {
        public DirectionalLight()
        {

        }

        public DirectionalLight(Vector3 direction)
        {
            Direction = direction;
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(Direction), Direction);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Direction = container.Read<Vector3>(nameof(Direction)); 
        }

        public Vector3 Direction { get; set; }
    }
}
