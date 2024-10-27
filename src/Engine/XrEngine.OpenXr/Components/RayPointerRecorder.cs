using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class RayPointerRecorder : Behavior<Scene3D>
    {
        public struct RecordFrame
        {
            public double Time;

            public RayPointerStatus Status;
        }

        public class RecordSession
        {
            public string? PointerName;

            public int PointerId;

            public IList<RecordFrame>? Frames;
        }


        RecordSession? _session;

        public RayPointerRecorder()
        {
            IsEnabled = false;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Pointer == null)
            {
                if (!string.IsNullOrWhiteSpace(PointerName))
                {
                    Pointer = _host!.Scene!
                        .Components<IRayPointer>()
                        .Where(a => a.Name == PointerName)
                        .FirstOrDefault();
                }

                if (Pointer == null)
                    return;
            }

            _session ??= new RecordSession
            {
                PointerName = Pointer.Name,
                PointerId = Pointer.PointerId,
                Frames = []
            };

            lock (this)
            {
                _session.Frames!.Add(new RecordFrame
                {
                    Time = ctx.Time,
                    Status = Pointer.GetPointerStatus()
                });
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

            File.WriteAllText(Path.Join(path, "pointer.json"), json);  
        }


        public RecordSession? Session => _session;

        public IRayPointer? Pointer { get; set; }    

        public string? PointerName { get; set; }  
    }
}
