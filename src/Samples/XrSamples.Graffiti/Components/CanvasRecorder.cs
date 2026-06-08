using System.Globalization;
using System.Numerics;
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
        protected Pose3 _lastCanvasPose;
        protected Vector2 _lastCanvasSize;
        protected double _startTime = 0;
        private bool _wasSpraying;

        public enum OpType
        {
            Spray,
            ChangeColor,
            Canvas,
            Params,
            SprayClose
        }

        public CanvasRecorder()
        {
            _outPath = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Recording");
            Directory.CreateDirectory(_outPath);
        }

        protected override void Update(RenderContext ctx)
        {
            _can ??= _host!.Descendants<Can>().First();
            _canvas ??= _host!.Descendants<PaintCanvas>().First();

            if (_can.SprayAperture > 0)
            {
                var canvasPose = _canvas.GetWorldPose();
                var canPose = _can.GetWorldPose();

                if (!_isRecording)
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
                        tracker.BaseDensity
                     );

                    _isRecording = true;
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
            if (!_isRecording)
                return;
            
            Write("\n]");

            _writer!.Close();
            _writer.Dispose();

            _writer = null;
            _entryCount = 0;
            _lastCanvasSize = Vector2.Zero;
            _lastCanvasPose = Pose3.Identity;
            _lastColor = Color.Transparent;
            _entryCount = 0;
            _wasSpraying = false;

            _isRecording = false;
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
                var fileName = Path.Combine(_outPath, $"Graffiti-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                var stream = File.Create(fileName);
                _writer = new StreamWriter(stream);
                _startTime = time;
            }

            if (!_isRecording)
                Write("[");

            if (_entryCount > 0)
                Write(",");

            Write("\n[");
            Write((int)type);
            Write(",");
            Write((float)(time - _startTime));

            var argI = 0;
            
            foreach (var p in args)
            {
                Write(",");
                Write(p);
                argI++;
            }

            Write("]");

            _entryCount++;
        }
    }
}
