using OpenXr.Framework;
using System.Text.Json;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class XrInputRecorder : Behavior<Scene3D>
    {
        public struct InputStatus
        {
            public bool IsActive { get; set; }

            public bool IsChanged { get; set; } 

            public object Value { get; set; }
        }

        public struct RecordFrame
        {
            public double Time;

            public Dictionary<string, InputStatus> Inputs;
        }

        public class RecordSession
        {
            public IList<RecordFrame>? Frames;
        }


        RecordSession? _session;

        public XrInputRecorder()
        {
            IsEnabled = false;
        }

        protected override void Update(RenderContext ctx)
        {
            if (XrApp.Current == null)
                return;

            _session ??= new RecordSession
            {
                Frames = []
            };

            lock (this)
            {
                var frame = new RecordFrame
                {
                    Time = ctx.Time,
                    Inputs = []
                };

                foreach (var input in XrApp.Current.Inputs.Values)
                    frame.Inputs[input.Name] = new InputStatus
                    {
                        IsChanged = input.IsChanged,
                        Value = input.Value,
                        IsActive = input.IsActive
                    };

                _session.Frames!.Add(frame);
            }
        }

        [Action]
        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };

            var path = Context.Require<IPlatform>().PersistentPath;

            string json;

            lock (this)
                json = JsonSerializer.Serialize(_session, options);

            File.WriteAllText(Path.Join(path, "inputs.json"), json);
        }


        public RecordSession? Session => _session;

        public IRayPointer? Pointer { get; set; }

        public string? PointerName { get; set; }
    }
}
