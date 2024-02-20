namespace OpenXr.Engine
{
    public class SpotLight : Light
    {
        public SpotLight()
        {
        }

        public float Range { get; set; }

        public float InnerConeAngle { get; set; }

        public float OuterConeAngle { get; set; }
    }
}
