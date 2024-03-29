using XrMath;

namespace XrEngine
{
    public class PointLight : Light
    {
        public PointLight()
        {
            Specular = Color.White;
            Range = 10;
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.WriteObject<PointLight>(this);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            container.ReadObject<PointLight>(this);
        }

        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
