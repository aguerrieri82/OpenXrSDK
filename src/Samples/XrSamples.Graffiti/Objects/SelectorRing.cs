using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public abstract class SelectorRing : Group3D
    {
        readonly List<Object3D> _buttons = [];
        readonly List<(PbrMaterial Material, Color Color)[]> _materials = [];

        protected readonly TriangleMesh _selectedMesh;
        protected float _visualIndex = -1;
        protected int _activeIndex;
        protected float _opacity;
        protected bool _isDirty;

        protected SelectorRing()
        {
            Radius = 0.10f;
            ButtonScale = 0.015f;
            MaxButtonsInCircle = 7;
            ArcRadians = MathF.PI;
            AnimationSpeed = 12.0f;

            _selectedMesh = AddChild(new TriangleMesh(
                new MeshBuilder()
                    .AddCircle(Vector3.Zero, 0.02f, 30)
                    .ToGeometry(),
                new ColorMaterial(new Color(1, 1, 0))));

            _selectedMesh.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
            _selectedMesh.Transform.Position = new Vector3(0, 0, Radius);

            Opacity = 1;
        }

        public override void Update(RenderContext ctx)
        {
            EnsureButtons();

            if (MathF.Abs(_visualIndex - _activeIndex) > 0.01f)
            {
                var t = 1.0f - MathF.Exp(-AnimationSpeed * (float)ctx.DeltaTime);

                _visualIndex += (_activeIndex - _visualIndex) * t;

                _isDirty = true;
            }

            if (_isDirty)
            {
                LayoutButtons();
                _isDirty = false;
            }

            base.Update(ctx);
        }

        public void SetActiveIndex(int index)
        {
            _activeIndex = Count == 0 ? 0 : Math.Clamp(index, 0, Count - 1);
        }

        protected abstract Object3D CreateButton(int index);

        void EnsureButtons()
        {
            while (_buttons.Count < Count)
            {
                var button = CreateButton(_buttons.Count);
                button.Transform.SetScale(ButtonScale);

                var materials = button.DescendantsOrSelf()
                    .OfType<TriangleMesh>()
                    .SelectMany(a => a.Materials)
                    .OfType<PbrMaterial>()
                    .ToArray();

                foreach (var mat in materials)
                    mat.Alpha = AlphaMode.Blend;

                _materials.Add(materials.Select(a => (a, a.Color)).ToArray());
                _buttons.Add(button);
                AddChild(button);

                _isDirty = true;
            }
        }

        void LayoutButtons()
        {
            var maxVisible = Math.Max(1, MaxButtonsInCircle);
            var halfSlots = (maxVisible - 1) * 0.5f;
            var angleStep = maxVisible <= 1 ? 0 : ArcRadians / (maxVisible - 1);

            for (var i = 0; i < Count; i++)
            {
                var button = _buttons[i];

                var rel = i - _visualIndex;
                var absRel = MathF.Abs(rel);

                var isVisible = absRel < halfSlots;

                button.IsVisible = isVisible;

                if (!isVisible)
                    continue;

                var angle = rel * angleStep;

                button.Transform.Position = new Vector3(
                    MathF.Sin(angle) * Radius,
                    0,
                    MathF.Cos(angle) * Radius);

                button.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle + MathF.PI);

                var normalizedAway = halfSlots <= 0 ? 0 : Math.Clamp(absRel / halfSlots, 0, 1);

                var alpha = MathF.Pow(1.0f - normalizedAway, 0.65f) * _opacity;

                foreach (var (mat, color) in _materials[i])
                {
                    mat.Color = new Color(color.R, color.G, color.B, color.A * alpha);
                    mat.Invalidate();
                }
            }
        }

        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                IsVisible = value > 0;
                _selectedMesh.IsVisible = value >= 1;
                _isDirty = true;
            }
        }

        public int ActiveIndex
        {
            get => _activeIndex;
            set => SetActiveIndex(value);
        }

        public abstract int Count { get; }

        public int MaxButtonsInCircle { get; set; }

        public float Radius { get; set; }

        public float ButtonScale { get; set; }

        public float ArcRadians { get; set; }

        public float AnimationSpeed { get; set; }
    }
}
