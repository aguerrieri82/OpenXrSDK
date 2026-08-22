using System.ComponentModel;
using System.Text.Json;

namespace XrEngine
{
    public class RecordFrame
    {
        public double Time;
    }

    public class RecordSession<TFrame>
    {
        public IList<TFrame>? Frames;
    }

    public abstract class BaseFrameRecorder<TFrame, TObj> : Behavior<TObj>, INotifyPropertyChanged
        where TObj : Object3D
        where TFrame : RecordFrame, new()
    {
        static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        };

        protected RecordSession<TFrame>? _session;

        public BaseFrameRecorder()
        {
            IsEnabled = false;
        }

        protected override void Update(RenderContext ctx)
        {
            _session ??= new()
            {
                Frames = []
            };

            lock (this)
            {
                var frame = new TFrame
                {
                    Time = ctx.Time
                };

                if (CreateFrame(frame))
                {
                    _session.Frames!.Add(frame);
                    OnPropertyChanged(nameof(FrameCount));
                }
            }
        }

        protected abstract bool CreateFrame(TFrame frame);

        [Action]
        public void Save()
        {
            var path = Context.Require<IPlatform>().PersistentPath;

            string json;

            lock (this)
                json = JsonSerializer.Serialize(_session, JSON_OPTIONS);

            File.WriteAllText(Path.Join(path, OutFile), json);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int FrameCount
        {
            get => _session?.Frames?.Count ?? 0;
            set { }
        }

        [ValueType(ValueType.FileName)]
        public string? OutFile { get; set; }

        public RecordSession<TFrame>? Session => _session;

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
