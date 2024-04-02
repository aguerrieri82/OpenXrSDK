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

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Direction), Direction);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Direction = container.Read<Vector3>(nameof(Direction)); 
        }

        public Vector3 Direction { get; set; }
    }
}
