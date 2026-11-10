using System.Numerics;
using XrMath;

namespace XrSamples.Graffiti
{
    public class SprayCap
    {
        public static readonly SprayCap ExtraSkinny = new()
        {
            Name = "Extra Skinny",
            SpreadAngle = 2.3f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 0.6f,
            BaseDensity = 3f,
            FlowExponent = 2f,
            Color = new Color(0.85f, 0.75f, 0.60f)
        };

        public static readonly SprayCap Skinny = new()
        {
            Name = "Skinny",
            SpreadAngle = 4.5f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 0.8f,
            BaseDensity = 5f,
            FlowExponent = 2f,
            Color = new Color(1.00f, 0.45f, 0.70f)
        };

        public static readonly SprayCap SoftSkinny = new()
        {
            Name = "Soft Skinny",
            SpreadAngle = 5.5f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 3f,
            BaseDensity = 5f,
            FlowExponent = 1.8f,
            Color = new Color(0.55f, 0.55f, 0.55f)
        };

        public static readonly SprayCap Stock = new()
        {
            Name = "Stock",
            SpreadAngle = MathF.PI / 20f,
            SprayRadius = 0.01f,
            RadialFalloff = 2f,
            BaseDensity = 8f,
            FlowExponent = 1.5f,
            Color = new Color(0.95f, 0.95f, 0.95f)
        };

        public static readonly SprayCap Fat = new()
        {
            Name = "Fat",
            SpreadAngle = 17f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 1.5f,
            BaseDensity = 16f,
            FlowExponent = 1.2f,
            Color = new Color(0.05f, 0.05f, 0.05f)
        };

        public static readonly SprayCap SuperFat = new()
        {
            Name = "Super Fat",
            SpreadAngle = 26f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 2.5f,
            BaseDensity = 24f,
            FlowExponent = 1.2f,
            Color = new Color(1.00f, 0.45f, 0.05f)
        };

        public static readonly SprayCap Calligraphy = new()
        {
            Name = "Calligraphy",
            SpreadAngle = 10f * MathF.PI / 180,
            SprayRadius = 0.01f,
            RadialFalloff = 1f,
            BaseDensity = 10f,
            FlowExponent = 1.5f,
            MaskSize = new Vector2(1f, 0.2f),
            Color = new Color(0.20f, 0.40f, 0.90f)
        };

        public static readonly SprayCap[] Presets = [ExtraSkinny, Skinny, SoftSkinny, Stock, Fat, SuperFat, Calligraphy];

        public string Name { get; init; } = "";

        public float SpreadAngle { get; init; }

        public float SprayRadius { get; init; }

        public float RadialFalloff { get; init; }

        public float BaseDensity { get; init; }

        public float FlowExponent { get; init; }

        public Vector2 MaskSize { get; init; } = Vector2.One;

        public float MaskAngle { get; init; }

        public Color Color { get; init; }
    }
}
