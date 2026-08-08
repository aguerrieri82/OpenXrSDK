using XrEngine.UI;
using XrMath;

namespace XrEngine.OpenXr
{
    public class ProfileOverlay : Behavior<Scene3D>
    {
        private CanvasView2D? _surface;
        private IGpuProfiler? _profiler;

        public ProfileOverlay()
        {
            PanelWidth = 0.45f;
            PanelHeight = 0.36f;
            PanelDistance = 0.82f;
            PanelOffsetX = 0f;
            PanelOffsetY = 0.18f;
        }

        protected override void Update(RenderContext ctx)
        {
            if (_surface == null)
            {
                _surface = _host.Children.OfType<CanvasView2D>().FirstOrDefault();

                if (_surface == null)
                {
                    _surface = new CanvasView2D();
                    _host.AddChild(_surface);
                }

                _surface.DrawCanvas += OnDraw;
            }

            base.Update(ctx);
        }

        private void OnDraw(ScreenCanvas obj)
        {
            _profiler ??= _host.App!.Renderer.Feature<IGpuProfiler>();

            if (_profiler == null || !_profiler.IsEnabled || !IsEnabled)
                return;

            var avgStats = _profiler.Averages;

            if (avgStats.Count == 0)
                return;

            const float Padding = 0.025f;
            const float HeaderHeight = 0.035f;
            const float FontHeight = 0.018f;
            const float ValueColumn = 0.68f;

            var camera = obj.Camera;

            if (camera == null)
                return;

            var right = camera.Right;
            var up = camera.Up;

            var center =
                camera.WorldPosition +
                camera.Forward * PanelDistance +
                right * PanelOffsetX +
                up * PanelOffsetY;

            var left = center - right * (PanelWidth * 0.5f);
            var top = center + up * (PanelHeight * 0.5f);

            var white = new Color(1, 1, 1, 1);
            var dim = new Color(0.6f, 0.6f, 0.6f, 1);
            var background = new Color(0, 0, 0, 0.65f);

            // Panel outline
            var topLeft = left + up * 0;
            var topRight = topLeft + right * PanelWidth;
            var bottomLeft = topLeft - up * PanelHeight;
            var bottomRight = topRight - up * PanelHeight;

            obj.DrawLine(topLeft, topRight, dim, UnitValue.Pixel(1));
            obj.DrawLine(topRight, bottomRight, dim, UnitValue.Pixel(1));
            obj.DrawLine(bottomRight, bottomLeft, dim, UnitValue.Pixel(1));
            obj.DrawLine(bottomLeft, topLeft, dim, UnitValue.Pixel(1));

            obj.DrawRect(
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
                background,
                dim,
                UnitValue.Pixel(1));

            // Header
            var headerPos =
                topLeft +
                right * Padding -
                up * Padding;

            obj.DrawText(
                "GPU PROFILE",
                headerPos,
                UnitValue.WorldY(FontHeight),
                white,
                Align.Start,
                Align.Start);

            var separatorY = Padding + HeaderHeight;

            obj.DrawLine(
                topLeft + right * Padding - up * separatorY,
                topRight - right * Padding - up * separatorY,
                dim,
                UnitValue.Pixel(1));

            var availableHeight =
                PanelHeight -
                separatorY -
                Padding;

            var rowHeight = availableHeight / avgStats.Count;

            var row = 0;

            foreach (var stat in avgStats)
            {
                var y =
                    separatorY +
                    rowHeight * (row + 0.5f);

                var namePos =
                    topLeft +
                    right * Padding -
                    up * y;

                var valuePos =
                    topLeft +
                    right * (PanelWidth * ValueColumn) -
                    up * y;

                obj.DrawText(
                    stat.Key,
                    namePos,
                    UnitValue.WorldY(FontHeight),
                    white,
                    Align.Start,
                    Align.Center);

                obj.DrawText(
                    $"{stat.Value:N1} us",
                    valuePos,
                    UnitValue.WorldY(FontHeight),
                    white,
                    Align.Start,
                    Align.Center);

                row++;
            }

        }

        [Range(0, 1, 0.01f)]
        public float PanelWidth { get; set; }

        [Range(0, 1, 0.01f)]
        public float PanelHeight { get; set; }

        [Range(0, 2, 0.01f)]
        public float PanelDistance { get; set; }

        [Range(-1, 1, 0.01f)]
        public float PanelOffsetX { get; set; }

        [Range(-1, 1, 0.01f)]
        public float PanelOffsetY { get; set; }

    }
}
