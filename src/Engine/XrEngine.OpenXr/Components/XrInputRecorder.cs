using OpenXr.Framework;
using System.Text.Json;

namespace XrEngine.OpenXr
{
    public class XrInputRecorder : Behavior<Scene3D>
    {


        #region RecordFrame

        public struct RecordFrame
        {
            public double Time;

            public long XrTime;

            public Dictionary<string, XrInputState> Inputs;
        }

        #endregion

        #region RecordSession

        public class RecordSession
        {
            public IList<RecordFrame>? Frames;
        }

        #endregion

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
                    XrTime = XrApp.Current.FramePredictedDisplayTime,
                    Inputs = []
                };

                foreach (var input in XrApp.Current.Inputs.Values)
                {
                    frame.Inputs[input.Name] = input.GetState();

                }
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
    }
}
