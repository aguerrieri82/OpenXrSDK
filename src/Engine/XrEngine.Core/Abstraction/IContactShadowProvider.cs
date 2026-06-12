namespace XrEngine
{

    public class ContactShadowOptions
    {
        public bool Use { get; set; }

        public float MaxDistance { get; set; }

        public float Thickness { get; set; }

        public float Strength { get; set; }

        public float StepCount { get; set; }

        public float DepthBias { get; set; }

        public float FadeDistance { get; set; }

        public float ApplyStrength { get; set; }

        public bool IsMultiView { get; set; }
    }


    public interface IContactShadowProvider
    {
        ContactShadowOptions Options { get; }
    }
}
