using OpenXr.Framework;
using Silk.NET.OpenXR;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public class AutoSave : Behavior<MainScene>
    {
        private PaintCanvas? _canvas;
        private CanvasRecorder? _recorder;
        private GraffitiStore? _store;
        private SpatialAnchorGrid? _grid;
        private double? _readyTime;
        private (string?, string?, int)? _lastSnapshot;
        private SessionState _lastSessionState;
        private double _lastSaveTime;
        private bool _isRestored;
        private Task? _task;

        public AutoSave()
        {
            IntervalSec = 30;
            AnchorTimeoutSec = 8;
        }

        protected override void Update(RenderContext ctx)
        {
            _canvas ??= _host.Descendants<PaintCanvas>().First();
            _recorder ??= _host.Component<CanvasRecorder>();
            _store ??= Context.Require<GraffitiStore>();

            if (_canvas.Simulation == null || _task?.IsCompleted == false)
                return;

            if (!_isRestored)
            {
                _grid ??= _canvas.Component<SpatialAnchorGrid>();
                _readyTime ??= ctx.Time;

                if (!_grid.IsLoaded && ctx.Time - _readyTime < AnchorTimeoutSec)
                    return;

                _isRestored = true;
                _lastSaveTime = ctx.Time;
                _task = _store.RestoreSessionAsync();
                return;
            }

            var sessionState = XrApp.Current?.SessionState ?? SessionState.Focused;
            var isFocusLost = _lastSessionState == SessionState.Focused && sessionState != SessionState.Focused;

            _lastSessionState = sessionState;

            if (!isFocusLost && ctx.Time - _lastSaveTime < IntervalSec)
                return;

            _lastSaveTime = ctx.Time;

            var snapshot = _recorder.Snapshot();

            if (_lastSnapshot == snapshot)
                return;

            _lastSnapshot = snapshot;
            _task = _store.SaveSessionAsync();
        }

        public float IntervalSec { get; set; }

        public float AnchorTimeoutSec { get; set; } 
    }
}
