using System.Numerics;
using XrEngine;

namespace XrSamples.Graffiti
{
    public enum PaintSelectorMode
    {
        Color,
        Cap
    }

    public class PaintSelector : Group3D
    {
        protected PaintSelectorMode _mode;
        protected float _transition;

        public PaintSelector()
        {
            SwapOffset = 0.04f;
            AnimationSpeed = 12.0f;

            ColorRing = AddChild(new ColorSelector());
            CapRing = AddChild(new CapSelector());

            Preview = AddChild(new CapPreview());
            Preview.Transform.Position = new Vector3(0, 0.055f, CapRing.Radius);

            Mode = PaintSelectorMode.Color;
            LayoutRings();
            IsVisible = false;
        }

        public override void Update(RenderContext ctx)
        {
            var target = _mode == PaintSelectorMode.Cap ? 1f : 0f;

            if (_transition != target)
            {
                _transition += (target - _transition) * (1.0f - MathF.Exp(-AnimationSpeed * (float)ctx.DeltaTime));

                if (MathF.Abs(target - _transition) < 0.01f)
                    _transition = target;

                LayoutRings();
            }

            base.Update(ctx);
        }

        void LayoutRings()
        {
            ColorRing.Transform.Position = new Vector3(0, -_transition * SwapOffset, 0);
            ColorRing.Opacity = 1 - _transition;

            CapRing.Transform.Position = new Vector3(0, (1 - _transition) * SwapOffset, 0);
            CapRing.Opacity = _transition;
        }

        public PaintSelectorMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Preview.IsVisible = value == PaintSelectorMode.Cap;
            }
        }

        public float SwapOffset { get; set; }

        public float AnimationSpeed { get; set; }

        public SelectorRing ActiveRing => _mode == PaintSelectorMode.Color ? ColorRing : CapRing;

        public ColorSelector ColorRing { get; }

        public CapSelector CapRing { get; }

        public CapPreview Preview { get; }
    }
}
