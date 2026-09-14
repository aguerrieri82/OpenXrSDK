using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;

namespace XrEngine.OpenXr
{
    public class PassthroughStyle : Behavior<Scene3D>
    {
        readonly Dictionary<ColorLut, XrColorLut> _xrLuts = [];

        XrPassthroughLayer? _layer;
        ColorLut? _lut;
        ColorLut? _targetLut;
        float _lutWeight = 1f;
        bool _styleDirty = true;

        protected override void Update(RenderContext ctx)
        {
            _layer ??= XrApp.Current?.Layers.List.OfType<XrPassthroughLayer>().FirstOrDefault();

            if (_layer == null || !_layer.IsEnabled)
                return;

            if (_styleDirty)
            {
                SyncLuts();

                _layer.SetStyle(new XrPassthroughStyle
                {
                    SourceLut = GetXrLut(_lut),
                    TargetLut = GetXrLut(_targetLut),
                    LutWeight = _lutWeight
                });

                _styleDirty = false;
            }

            base.Update(ctx);
        }

        private void SyncLuts()
        {
            var active = new HashSet<ColorLut>();

            if (_lut != null)
                active.Add(_lut);

            if (_targetLut != null)
                active.Add(_targetLut);

            foreach (var lut in active)
            {
                if (!_xrLuts.ContainsKey(lut))
                    _xrLuts[lut] = CreateXrLut(lut);
            }

            foreach (var lut in _xrLuts.Keys.Where(x => !active.Contains(x)).ToArray())
            {
                _xrLuts[lut].Dispose();
                _xrLuts.Remove(lut);
            }
        }

        private XrColorLut CreateXrLut(ColorLut lut)
        {
            return _layer!.CreateColorLut(
                (uint)lut.Resolution,
                PassthroughColorLutChannelsMETA.RgbMeta,
                lut.BuildData());
        }

        private PassthroughColorLutMETA? GetXrLut(ColorLut? lut)
        {
            if (lut == null)
                return null;

            return _xrLuts[lut];
        }

        public ColorLut? Lut
        {
            get => _lut;
            set
            {
                if (_lut == value)
                    return;

                _lut = value;
                _styleDirty = true;
            }
        }

        public ColorLut? TargetLut
        {
            get => _targetLut;
            set
            {
                if (_targetLut == value)
                    return;

                _targetLut = value;
                _styleDirty = true;
            }
        }

        [Range(0, 1, 0.01f)]
        public float LutWeight
        {
            get => _lutWeight;
            set
            {
                if (_lutWeight == value)
                    return;

                _lutWeight = value;
                _styleDirty = true;
            }
        }
    }
}