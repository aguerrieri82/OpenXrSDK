using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class PaintSelector : Group3D
    {
        readonly List<TriangleMesh> _buttons = [];
        readonly List<PbrV2Material> _materials = [];

        protected Geometry3D? _buttonGeometry;
        protected readonly TriangleMesh _selectedMesh;
        protected float _visualIndex = -1;
        protected int _activeIndex = 2;

        public PaintSelector()
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

            Radius = 0.10f;
            ButtonScale = 0.015f;
            MaxButtonsInCircle = 7;
            ArcRadians = MathF.PI;
            AnimationSpeed = 12.0f;
            IsVisible = false;

            var source = (TriangleMesh)AssetLoader.Instance.Load(
                new Uri("res://asset/Button.obj"),
                typeof(TriangleMesh),
                null);

            source.Geometry!.ComputeIndices();

            _buttonGeometry = source.Geometry;

            _selectedMesh = AddChild(new TriangleMesh(
                new MeshBuilder()
                    .AddCircle(Vector3.Zero, 0.02f, 30)
                    .ToGeometry(),
                new ColorMaterial(new Color(1, 1, 0))));

            _selectedMesh.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
            _selectedMesh.Transform.Position = new Vector3(
                   0,
                   0,
                   Radius);
        }

        public override void Update(RenderContext ctx)
        {
            EnsureButtons();


            if (MathF.Abs(_visualIndex - _activeIndex) > 0.01f && IsVisible)
            {
                var dt = (float)ctx.DeltaTime;
                var t = 1.0f - MathF.Exp(-AnimationSpeed * dt);

                _visualIndex = Lerp(_visualIndex, _activeIndex, t);

                LayoutButtons();
            }


            base.Update(ctx);
        }

        public void SetActiveIndex(int index)
        {
            if (Colors.Count == 0)
            {
                _activeIndex = 0;
                _visualIndex = 0;
                return;
            }

            _activeIndex = Math.Clamp(index, 0, Colors.Count - 1);
        }

        void EnsureButtons()
        {
            if (_buttonGeometry == null)
                return;

            while (_buttons.Count < Colors.Count)
            {
                var mat = new PbrV2Material
                {
                    Color = new Color(1, 1, 1, 1),
                    Metalness = 0,
                    Roughness = 0.12f,
                    Alpha = AlphaMode.Blend
                };

                var button = new TriangleMesh(_buttonGeometry);
                button.Transform.SetScale(ButtonScale);
                button.Materials.Add(mat);

                _materials.Add(mat);
                _buttons.Add(button);
                AddChild(button);
            }

            for (var i = Colors.Count; i < _buttons.Count; i++)
            {
                _materials[i].Color = Color.Transparent;
                _materials[i].IsEnabled = false;
            }
        }

        void LayoutButtons()
        {
            if (Colors.Count == 0)
                return;

            var maxVisible = Math.Max(1, MaxButtonsInCircle);
            var halfSlots = (maxVisible - 1) * 0.5f;

            var angleStep =
                maxVisible <= 1
                    ? 0
                    : ArcRadians / (maxVisible - 1);

            for (var i = 0; i < Colors.Count; i++)
            {
                var button = _buttons[i];
                var mat = _materials[i];

                var rel = i - _visualIndex;

                var absRel = MathF.Abs(rel);

                if (absRel > halfSlots + 1.0f)
                    mat.Color = WithAlpha(Colors[i], 0);
                else
                {
                    var angle = rel * angleStep;

                    // Plane XZ, Y up.
                    // Center button is at angle 0, directly forward on +Z.
                    var pos = new Vector3(
                        MathF.Sin(angle) * Radius,
                        0,
                        MathF.Cos(angle) * Radius);

                    button.Transform.Position = pos;

                    var normalizedAway = halfSlots <= 0 ? 0 : absRel / halfSlots;
                    normalizedAway = Math.Clamp(normalizedAway, 0, 1);

                    // Center = opaque, sides = faded.
                    var alpha = 1.0f - normalizedAway;
                    alpha = MathF.Pow(alpha, 0.65f);

                    mat.Color = WithAlpha(Colors[i], Colors[i].A * alpha);
                }

                mat.IsEnabled = true;
                mat.ContentVersion++;
            }
        }

        static Color WithAlpha(Color c, float a)
        {
            c.A = a;
            return c;
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }


        public uint ActiveIndex
        {
            get => (uint)_activeIndex;
            set => SetActiveIndex((int)value);
        }

        public IList<Color> Colors { get; set; }

        public int MaxButtonsInCircle { get; set; }

        public float Radius { get; set; }

        public float ButtonScale { get; set; }

        public float ArcRadians { get; set; }

        public float AnimationSpeed { get; set; }
    }
}