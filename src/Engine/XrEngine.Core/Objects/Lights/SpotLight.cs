namespace XrEngine
{
    public class SpotLight : Light
    {
        public SpotLight()
        {
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(Range), Range);
            container.Write(nameof(InnerConeAngle), InnerConeAngle);
            container.Write(nameof(OuterConeAngle), OuterConeAngle);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Range = container.Read<float>(nameof(Range));
            InnerConeAngle = container.Read<float>(nameof(InnerConeAngle));
            OuterConeAngle = container.Read<float>(nameof(OuterConeAngle));
        }

        public float Range { get; set; }

        public float InnerConeAngle { get; set; }

        public float OuterConeAngle { get; set; }
    }
}
