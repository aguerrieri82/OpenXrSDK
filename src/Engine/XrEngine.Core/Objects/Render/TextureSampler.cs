using XrMath;

namespace XrEngine
{
    public class TextureSampler : EngineObject
    {
        public TextureSampler()
        {
            MinFilter = ScaleFilter.Linear;
            MagFilter = ScaleFilter.Linear;

            WrapS = WrapMode.Repeat;
            WrapT = WrapMode.Repeat;
            WrapR = WrapMode.Repeat;

            BorderColor = new Color(0, 0, 0, 0);

            MinLod = -1000f;
            MaxLod = 1000f;
            LodBias = 0;

            MaxAnisotropy = 0;

            DecodeSrgb = true;
        }

        public ScaleFilter MinFilter { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public WrapMode WrapS { get; set; }

        public WrapMode WrapT { get; set; }

        public WrapMode WrapR { get; set; }

        public Color BorderColor { get; set; }

        public float MinLod { get; set; }

        public float MaxLod { get; set; }

        public float LodBias { get; set; }

        public float MaxAnisotropy { get; set; }

        public bool DecodeSrgb { get; set; }
    }
}