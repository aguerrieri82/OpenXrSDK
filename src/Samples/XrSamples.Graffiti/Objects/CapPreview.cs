using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CapPreview : Group3D
    {
        private readonly CapPreviewMaterial _front;
        private readonly CapPreviewMaterial _side;
        private readonly CapLabel _label;
        private float _distance;
        private Can? _can;
        private SprayCap? _cap;

        public CapPreview()
        {
            _front = new CapPreviewMaterial();
            _side = new CapPreviewMaterial { IsSide = true };

            var front = AddChild(new TriangleMesh(new Quad3D(new Vector2(0.06f, 0.06f)), _front));
            front.Transform.Position = new Vector3(0.045f, 0, 0);

            var side = AddChild(new TriangleMesh(new Quad3D(new Vector2(0.08f, 0.04f)), _side));
            side.Transform.Position = new Vector3(-0.055f, -0.01f, 0);

            _label = AddChild(new CapLabel { Size = new Size2(0.18f, 0.02f) });
            _label.Transform.Position = new Vector3(-0.005f, 0.045f, 0);

            Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);

            Distance = 0.15f;
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            if (_scene != null)
            {
                _can ??= _scene!.Descendants<Can>().First();

                if (_can.Cap != _cap)
                {
                    _cap = _can.Cap;
                    UpdateText();
                }
            }

            base.UpdateSelf(ctx);
        }


        void UpdateText()
        {
            if (_cap == null)
                return;

            var width = 2 * (_cap.SprayRadius * _can!.Transform.Scale.X + _distance * MathF.Tan(_cap.SpreadAngle));
            var size = Vector2.Min(_cap.MaskSize, Vector2.One) * width * 100;

            var sizeText = size.X == size.Y ? $"Ø {size.X:0.0}" : $"{size.X:0.0} × {size.Y:0.0}";

            _label.Text = $"{_cap.Name}   {_distance * 100:0} cm   {sizeText} cm";
        }

        public float Distance
        {
            get => _distance;
            set
            {
                _distance = Math.Clamp(value, 0.05f, 0.5f);
                _front.Distance = _distance;
                _side.Distance = _distance;
                UpdateText();
            }
        }
    }
}
