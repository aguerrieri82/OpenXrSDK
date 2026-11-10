using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public abstract class SelectorRing : Group3D
    {
        readonly List<TriangleMesh> _buttons = [];
        readonly List<PbrMaterial> _materials = [];

        protected Geometry3D? _buttonGeometry;
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

        protected abstract Color GetColor(int index);

        void EnsureButtons()
        {
            if (_buttonGeometry == null)
                return;

            while (_buttons.Count < Count)
            {
                var mat = new PbrMaterial
                {
                    Color = new Color(1, 1, 1, 1),
                    Metalness = 0,
                    Roughness = 0.12f,
                    Alpha = AlphaMode.Blend,
                    DoubleSided = true
                };

                var button = new TriangleMesh(_buttonGeometry);
                button.Transform.SetScale(ButtonScale);
                button.Materials.Add(mat);

                _materials.Add(mat);
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
                var mat = _materials[i];
                var color = GetColor(i);

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

                var normalizedAway = halfSlots <= 0 ? 0 : Math.Clamp(absRel / halfSlots, 0, 1);

                color.A *= MathF.Pow(1.0f - normalizedAway, 0.65f) * _opacity;

                mat.Color = color;
                mat.Invalidate();
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
