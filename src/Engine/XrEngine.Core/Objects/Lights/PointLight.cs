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
            container.Write(nameof(Range), Range);
            container.Write(nameof(Specular), Specular);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Range = container.Read<float>(nameof(Range));
            Specular = container.Read<Color>(nameof(Specular));
        }

        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
