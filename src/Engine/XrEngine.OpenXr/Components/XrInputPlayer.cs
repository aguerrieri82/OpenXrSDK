﻿
using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.ComponentModel;
using System.Numerics;
using System.Text.Json;

using XrMath;

namespace XrEngine.OpenXr
{
    public class XrInputPlayer : Behavior<Scene3D>, INotifyPropertyChanged, IPlayer, IDrawGizmos
    {
        XrInputRecorder.RecordSession? _session;
        XrInputRecorder.RecordFrame _frame;
        int _frameNum;
        PlayerState _state;
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

        protected override void Update(RenderContext ctx)
        {
            if (_state == PlayerState.Play)
                Frame++;
        }

        protected void LoadFrame()
        {
            if (XrApp.Current == null)
                return;

            if (_session?.Frames == null)
                return;

            if (_frameNum < 0 || _frameNum >= _session.Frames.Count)
                return;

            _frame = _session.Frames[Frame];

            foreach (var input in _frame.Inputs)
            {
                var xrInput = XrApp.Current.Inputs[input.Key];
                xrInput?.SetState(input.Value);

                if (input.Key == "RightGripPose")
                {
                    var pose = ((XrPoseInput)xrInput!).Value;
                    Log.Value("Pose-X", MathF.Round(pose.Position.X, 5));
                }
            }
        }

        [Action]
        public async Task LoadAsync()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };

            var path = Context.Require<IPlatform>().PersistentPath;

            using var stream = File.OpenRead(Path.Join(path, SourceFile));

            _session = await JsonSerializer.DeserializeAsync<XrInputRecorder.RecordSession>(stream, options);

            Frame = 0;

            OnPropertyChanged(nameof(Length));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(SourceFile), SourceFile);
            container.Write(nameof(ShowTrail), ShowTrail);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            SourceFile = container.Read<string>(nameof(SourceFile));
            ShowTrail = container.Read<bool>(nameof(ShowTrail));
        }

        public void SetPlayState(PlayerState state)
        {
            _state = state;

            if (state == PlayerState.Stop)
                Frame = 0;
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            if (_session?.Frames == null || !ShowTrail)
                return;

            var DELTA = 10;

            var min = Math.Max(0, _frameNum - DELTA);
            var max = Math.Min(Length - 1, _frameNum + DELTA);

            var prevPoint = Vector3.Zero;

            canvas.Save();

            AdvancePosePredictor pre0 = new();

            for (var i = min; i <= max; i++)
            {
                var frame = _session.Frames[i];
                var alpha = 1 - (Math.Abs(i - _frameNum) / (float)DELTA);

                if (!frame.Inputs.TryGetValue("RightGripPose", out var pose))
                    continue;
                var value = pose.Value;
                if (value is JsonElement je)
                    value = je.Deserialize<Pose3>(new JsonSerializerOptions { IncludeFields = true })!;

                var curPose = (Pose3)value;

                if (prevPoint != Vector3.Zero)
                {
                    if (i < _frameNum)
                        canvas.State.Color = new Color(0, 0, 1, alpha);
                    else
                        canvas.State.Color = new Color(1, 0, 0, alpha);

                    canvas.DrawLine(prevPoint, curPose.Position);

                    canvas.DrawCircle((Pose3)value, 0.002f, 10);
                }

                prevPoint = curPose.Position;

                pre0.Track(curPose, (float)frame.Time);
                _predictor!.Track(curPose, (float)frame.Time);

                if (i == _frameNum && i > 10)
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

            canvas.Restore();
        }

        public int Frame
        {
            get => _frameNum;
            set
            {
                value = Math.Min(Math.Max(0, value), Length - 1);

                if (value == _frameNum)
                    return;

                _frameNum = value;

                LoadFrame();

                OnPropertyChanged(nameof(Frame));
            }
        }



        public bool ShowTrail { get; set; }

        public PlayerState PlayState => _state;

        public int Length => _session?.Frames?.Count ?? 0;


        [ValueType(ValueType.FileName)]
        public string? SourceFile { get; set; }


        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
