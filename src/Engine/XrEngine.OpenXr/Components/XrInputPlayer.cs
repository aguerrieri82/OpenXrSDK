using OpenXr.Framework;
using System.ComponentModel;
using System.Text.Json;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class XrInputPlayer : Behavior<Scene3D>, INotifyPropertyChanged, IPlayer
    {
        XrInputRecorder.RecordSession? _session;
        XrInputRecorder.RecordFrame _frame;
        int _frameNum;
        PlayerState _state;

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
                xrInput?.ForceState(input.Value.IsChanged, input.Value.IsActive, input.Value.Value);
            }

        }

        [Action]
        public void Load()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };

            var path = Context.Require<IPlatform>().PersistentPath;

            var json = File.ReadAllText(Path.Join(path, "inputs.json"));

            _session = JsonSerializer.Deserialize<XrInputRecorder.RecordSession>(json, options);

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
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            SourceFile = container.Read<string>(nameof(SourceFile));
        }

        public void SetPlayState(PlayerState state)
        {
            _state = state;

            if (state == PlayerState.Stop)
                Frame = 0;
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

        public PlayerState PlayerState => _state;


        public int Length => _session?.Frames?.Count ?? 0;  


        [ValueType(ValueType.FileName)]
        public string? SourceFile { get; set; }


        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
