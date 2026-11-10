using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class ColorSelector : SelectorRing
    {
        public ColorSelector()
        {
            Colors =
            [
                new Color(1.00f, 1.00f, 1.00f, 1.0f), // White
                new Color(0.02f, 0.02f, 0.02f, 1.0f), // Black

                new Color(0.85f, 0.02f, 0.02f, 1.0f), // Red
                new Color(1.00f, 0.20f, 0.02f, 1.0f), // Orange-red
                new Color(1.00f, 0.48f, 0.02f, 1.0f), // Orange
                new Color(1.00f, 0.82f, 0.02f, 1.0f), // Yellow

                new Color(0.45f, 0.85f, 0.04f, 1.0f), // Lime green
                new Color(0.02f, 0.55f, 0.12f, 1.0f), // Green
                new Color(0.00f, 0.75f, 0.55f, 1.0f), // Teal

                new Color(0.02f, 0.45f, 0.95f, 1.0f), // Blue
                new Color(0.02f, 0.12f, 0.70f, 1.0f), // Deep blue
                new Color(0.35f, 0.05f, 0.75f, 1.0f), // Purple

                new Color(0.95f, 0.05f, 0.65f, 1.0f), // Magenta
                new Color(1.00f, 0.35f, 0.70f, 1.0f), // Pink

                new Color(0.45f, 0.24f, 0.08f, 1.0f), // Brown
                new Color(0.62f, 0.62f, 0.62f, 1.0f), // Gray
            ];

            _activeIndex = 2;

            var source = (TriangleMesh)AssetLoader.Instance.Load(
                new Uri("res://asset/Button.obj"),
                typeof(TriangleMesh),
                null);

            source.Geometry!.ComputeIndices();

            _buttonGeometry = source.Geometry;
        }

        protected override Color GetColor(int index) => Colors[index];

        public Color ActiveColor => Colors[_activeIndex];

        public IList<Color> Colors { get; set; }

        public override int Count => Colors.Count;
    }
}
