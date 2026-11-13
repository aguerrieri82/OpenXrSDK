using System.Globalization;
using System.Numerics;
using System.Text.Json;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CanvasRecorder : Behavior<MainScene>
    {
        protected readonly string _outPath;
        protected bool _isRecording;
        protected Can? _can;
        protected PaintCanvas? _canvas;
        protected int _entryCount;
        protected StreamWriter? _writer;
        protected Color _lastColor;
        protected SprayCap? _lastCap;
        protected Pose3 _lastCanvasPose;
        protected Vector2 _lastCanvasSize;
        protected double _startRecordTime = 0;
        protected string? _fileName;
        protected string _history = "[";
        protected float _historyTime;
        protected int _historyCount;
        private bool _wasSpraying;

        public enum OpType
        {
            Spray,
            ChangeColor,
            Canvas,
            Params,
            SprayClose,
            Undo,
            Clear
        }

        public CanvasRecorder()
        {
            UpdatePriority = 1;

            _outPath = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Recording");
            Directory.CreateDirectory(_outPath);
        }

        protected override void Update(RenderContext ctx)
        {
            _can ??= _host.Descendants<Can>().First();

            if (_canvas == null)
            {
                _canvas = _host.Descendants<PaintCanvas>().First();
                _canvas.Undone += () => Append(OpType.Undo, ((IReferenceTime)EngineApp.Current!).Time);
                _canvas.Cleared += () => Append(OpType.Clear, ((IReferenceTime)EngineApp.Current!).Time);
            }

            if (_can.SprayAperture > 0 && _host.ActiveTool != GraffitiTool.PaintSelector)
            {
                var canvasPose = _canvas.GetWorldPose();
                var canPose = _can.GetWorldPose();

                if (!_isRecording || _lastCap != _can.Cap)
                {
                    var tracker = _can.Component<SprayTracker>();

                    Append(OpType.Params, ctx.Time,
                        _canvas.DryRoughness,
                        _canvas.WetRoughness,
                        _canvas.NormalScale,
                        _canvas.DryRate,
                        _canvas.DripRate,
                        _canvas.PaintOpacityScale,

                        tracker.SpreadAngle,
                        tracker.SprayCenter,
                        tracker.SprayDirection,
                        tracker.SprayRadius,
                        tracker.RadialFalloff,
                        tracker.BaseDensity,
                        tracker.FlowExponent,
                        tracker.MaskSize,
                        tracker.MaskAngle
                     );

                    _isRecording = true;
                    _lastCap = _can.Cap;
                }

                if (_lastCanvasSize != _canvas.Size || _lastCanvasPose != canvasPose)
                {
                    Append(OpType.Canvas, ctx.Time, _canvas.Size, canvasPose);
                    _lastCanvasSize = _canvas.Size;
                    _lastCanvasPose = canvasPose;
                }

                if (_lastColor != _can.Color)
                {
                    Append(OpType.ChangeColor, ctx.Time, _can.Color);
                    _lastColor = _can.Color;
                }

                Append(OpType.Spray, ctx.Time, canPose, _can.SprayAperture);

                _wasSpraying = true;
            }
            else if (_wasSpraying)
            {
                Append(OpType.SprayClose, ctx.Time);
                _wasSpraying = false;
            }

            base.Update(ctx);
        }

        void Write(string data)
        {
            _writer!.Write(data);
        }

        void Write(Pose3 data)
        {
            Write("[");
            Write(data.Position);
            Write(",");
            Write(data.Orientation);
            Write("]");
        }

        void Write(Quaternion data)
        {
            Write("[");
            Write(data.X);
            Write(",");
            Write(data.Y);
            Write(",");
            Write(data.Z);
            Write(",");
            Write(data.W);
            Write("]");
        }

        void Write(Vector3 data)
        {
            Write("[");
            Write(data.X);
            Write(",");
            Write(data.Y);
            Write(",");
            Write(data.Z);
            Write("]");
        }

        void Write(Color data)
        {
            Write("[");
            Write(data.R);
            Write(",");
            Write(data.G);
            Write(",");
            Write(data.B);
            Write(",");
            Write(data.A);
            Write("]");
        }

        void Write(Vector2 data)
        {
            Write("[");
            Write(data.X);
            Write(",");
            Write(data.Y);
            Write("]");
        }

        void Write(float data)
        {
            Write(data.ToString(CultureInfo.InvariantCulture));
        }

        void Write(object data)
        {
            if (data is Pose3 pose)
                Write(pose);
            else if (data is Vector3 vec3)
                Write(vec3);
            else if (data is Quaternion quat)
                Write(quat);
            else if (data is Vector2 vec2)
                Write(vec2);
            else if (data is float num)
                Write(num);
            else if (data is int iNum)
                Write(iNum);
            else if (data is Color color)
                Write(color);
            else
                throw new NotSupportedException();
        }

        [Action]
        public void StopRecord()
        {
            if (_writer == null)
                return;

            Write("\n]");

            _writer!.Close();
            _writer.Dispose();

            _writer = null;
            _entryCount = 0;
            _lastCanvasSize = Vector2.Zero;
            _lastCanvasPose = Pose3.Identity;
            _lastColor = Color.Transparent;
            _lastCap = null;
            _wasSpraying = false;

            _isRecording = false;
        }

        public void Resume(string json, int count = int.MaxValue)
        {
            StopRecord();

            var entries = json.Split('\n')
                .Select(a => a.TrimEnd().TrimEnd(','))
                .Where(a => a.Length > 1 && a[0] == '[')
                .Take(count)
                .ToArray();

            _history = "[" + string.Join(",", entries.Select(a => "\n" + a));
            _historyCount = entries.Length;
            _historyTime = 0;

            if (_historyCount > 0)
            {
                using var last = JsonDocument.Parse(entries[^1]);
                _historyTime = last.RootElement[1].GetSingle();
            }
        }

        public (string? FilePath, string? History, int Count) Snapshot()
        {
            return _writer != null ? (_fileName, null, _entryCount) : (null, _history, _historyCount);
        }

        public void SaveTo(string filePath)
        {
            if (_writer != null)
            {
                StopRecord();
                Resume(File.ReadAllText(_fileName!));
            }

            File.WriteAllText(filePath, _history + "\n]");
        }

        protected override void OnDisabled()
        {
            StopRecord();
            base.OnDisabled();
        }

        protected void Append(OpType type, double time, params object[] args)
        {
            if (_writer == null)
            {
                _fileName = Path.Combine(_outPath, $"Graffiti-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                _writer = new StreamWriter(new FileStream(_fileName, FileMode.Create, FileAccess.Write, FileShare.Read));
                _startRecordTime = time - _historyTime;
                _entryCount = _historyCount;
                Write(_history);
            }

            if (_entryCount > 0)
                Write(",");

            Write("\n[");
            Write((int)type);
            Write(",");
            Write((float)(time - _startRecordTime));

            var argI = 0;

            foreach (var p in args)
            {
                Write(",");
                Write(p);
                argI++;
            }

            Write("]");

            _writer.Flush();

            _entryCount++;
        }
    }
}
