namespace XrEngine
{
    public class SunLight : DirectionalLight
    {
        public SunLight()
        {
            HaloSize = 10;
            HaloFallOff = 80;
            SunRadius = 1.9f;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<SunLight>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<SunLight>(this);
        }

        public float HaloSize { get; set; }

        public float HaloFallOff { get; set; }

        public float SunRadius { get; set; }

    }
}
