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
            container.WriteObject<SpotLight>(this);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            container.ReadObject<SpotLight>(this);
        }

        public float Range { get; set; }

        public float InnerConeAngle { get; set; }

        public float OuterConeAngle { get; set; }
    }
}
