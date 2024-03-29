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

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            container.Write(nameof(HaloSize), HaloSize);
            container.Write(nameof(HaloFallOff), HaloFallOff);
            container.Write(nameof(SunRadius), SunRadius);
        }
        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            HaloSize = container.Read<float>(nameof(HaloSize));
            HaloFallOff = container.Read<float>(nameof(HaloFallOff));
            SunRadius = container.Read<float>(nameof(SunRadius));
        }

        public float HaloSize { get; set; }
        
        public float HaloFallOff { get; set; }

        public float SunRadius { get; set; }

    }
}
