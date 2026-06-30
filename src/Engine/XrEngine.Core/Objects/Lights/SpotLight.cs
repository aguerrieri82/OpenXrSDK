namespace XrEngine
{
    public class SpotLight : Light
    {
        public SpotLight()
        {
            Range = 6.0f;
            Intensity = 1.0f;
            InnerConeAngle = MathF.PI / 10.0f; // 18°
            OuterConeAngle = MathF.PI / 6.0f;  // 30°
            Color = "#ffffff";
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


        [ValueType(ValueType.Radiant)]
        public float InnerConeAngle { get; set; }


        [ValueType(ValueType.Radiant)]
        public float OuterConeAngle { get; set; }
    }
}
