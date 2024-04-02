namespace XrEngine
{
    public class SpotLight : Light
    {
        public SpotLight()
        {
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<SpotLight>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<SpotLight>(this);
        }

        public float Range { get; set; }

        public float InnerConeAngle { get; set; }

        public float OuterConeAngle { get; set; }
    }
}
