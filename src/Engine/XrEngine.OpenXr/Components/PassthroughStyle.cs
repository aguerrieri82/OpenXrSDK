using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;

namespace XrEngine.OpenXr
{
    public class PassthroughStyle : Behavior<Scene3D>
    {
        ColorLut? _lastLut;
        XrColorLut? _xrLut;
        private XrPassthroughLayer? _layer;

        public PassthroughStyle()
        {
            LutWeight = 1f;
        }

        protected override void Update(RenderContext ctx)
        {
            _layer ??= XrApp.Current?.Layers.List.OfType<XrPassthroughLayer>().FirstOrDefault();

            if (_layer == null || !_layer.IsEnabled)
                return;

            if (_lastLut != Lut)
            {
                _xrLut?.Dispose();

                if (Lut != null)
                {
                    _xrLut = _layer.CreateColorLut(
                        (uint)Lut.Resolution,
                        PassthroughColorLutChannelsMETA.RgbMeta,
                        Lut.BuildData());
                }
                else
                    _xrLut = null;

                _layer.SetStyle(new XrPassthroughStyle
                {
                    Lut = _xrLut?.Handle,
                    LutWeight = LutWeight
                });

                _lastLut = Lut;
            }

            base.Update(ctx);
        }

        public ColorLut? Lut { get; set; }

        [Range(0,1, 0.01f)]
        public float LutWeight { get; set; }
    }
}
