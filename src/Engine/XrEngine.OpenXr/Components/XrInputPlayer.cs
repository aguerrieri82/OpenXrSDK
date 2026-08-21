using OpenXr.Framework;
using System.Numerics;
using System.Text.Json;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrInputPlayer : BaseFramePlayer<XrInputRecorder.XrRecordFrame, Object3D>, IDrawGizmos
    {
        readonly IPosePredictor? _predictor;

        public XrInputPlayer()
            : this(null)
        {
        }

        public XrInputPlayer(IPosePredictor? predictor)
        {
            _predictor = predictor;
            SourceFile = "inputs.json";
        }

        protected override void ApplyFrame(XrInputRecorder.XrRecordFrame frame)
        {
            if (XrApp.Current == null)
                return;

            foreach (var input in frame.Inputs!)
            {
                var xrInput = XrApp.Current.Inputs[input.Key];
                xrInput?.SetState(input.Value);
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(ShowTrail), ShowTrail);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            ShowTrail = container.Read<bool>(nameof(ShowTrail));
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (_session?.Frames == null || !ShowTrail)
                return;

            const int DELTA = 10;

            var min = Math.Max(0, _frameNum - DELTA);
            var max = Math.Min(Length - 1, _frameNum + DELTA);
            var prevPoint = Vector3.Zero;

            canvas.Save();

            AdvancePosePredictor pre0 = new();

            for (var i = min; i <= max; i++)
            {
                var frame = _session.Frames[i];
                var alpha = 1 - Math.Abs(i - _frameNum) / (float)DELTA;

                if (!frame.Inputs!.TryGetValue("RightGripPose", out var pose))
                    continue;

                var value = pose.Value;

                if (value is JsonElement je)
                    value = je.Deserialize<Pose3>(new JsonSerializerOptions { IncludeFields = true })!;

                var curPose = (Pose3)value;

                if (prevPoint != Vector3.Zero)
                {
                    canvas.State.Color = i < _frameNum ? new Color(0, 0, 1, alpha) : new Color(1, 0, 0, alpha);
                    canvas.DrawLine(prevPoint, curPose.Position);
                    canvas.DrawCircle(curPose, 0.002f, 10);
                }

                prevPoint = curPose.Position;

                pre0.Track(curPose, (float)frame.Time);

                if (_predictor != null)
                {
                    _predictor.Track(curPose, (float)frame.Time);

                    if (i == _frameNum && i > 10 && i + 5 < _session.Frames.Count)
                    {
                        var maxTime = _session.Frames[i + 5].Time;
                        var pdt = maxTime - (float)frame.Time;

                        var pp0 = _predictor.Predict((float)pdt);
                        var pp1 = pre0.Predict((float)pdt);

                        canvas.State.Color = new Color(1, 1, 0);
                        canvas.DrawCircle(pp0, 0.002f, 10);

                        canvas.State.Color = new Color(0, 1, 1);
                        canvas.DrawCircle(pp1, 0.002f, 10);
                    }
                }
            }

            canvas.Restore();
        }

        public bool ShowTrail { get; set; }
    }
}