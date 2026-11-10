using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CapSelector : SelectorRing
    {
        public CapSelector()
        {
            ButtonScale = 0.01f;
            Caps = SprayCap.Presets;
            _activeIndex = Caps.IndexOf(SprayCap.Stock);

            var profile = new Poly2D
            {
                Points =
                [
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 0.8f),
                    new Vector2(0.9f, 1.0f),
                    new Vector2(0.6f, 1.15f),
                    new Vector2(0, 1.2f)
                ]
            };

            _buttonGeometry = new MeshBuilder()
                .AddRevolve(profile, 24, UVMode.Normalized)
                .ToGeometry();
        }

        protected override Color GetColor(int index) => Caps[index].Color;

        public SprayCap ActiveCap => Caps[_activeIndex];

        public IList<SprayCap> Caps { get; set; }

        public override int Count => Caps.Count;
    }
}
