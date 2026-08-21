using XrMath;

namespace XrEngine
{
    public enum TexCompareFunc
    {
        Never = 0x200,

        Less = 0x201,

        Equal = 0x202,

        Lequal = 0x203,

        Greater = 0x204,

        Notequal = 0x205,

        Gequal = 0x206,

        Always = 0x207,
    }

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
            CompareFunc = TexCompareFunc.Lequal;
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

        public TexCompareFunc CompareFunc { get; set; }

        public bool UseTexCompare { get; set; }

    }
}