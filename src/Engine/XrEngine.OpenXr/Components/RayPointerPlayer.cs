using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class RayPointerPlayer : Behavior<Scene3D>, IRayPointer
    {
        RayPointerRecorder.RecordSession? _session;
        RayPointerRecorder.RecordFrame _frame;
        private bool _isCaptured;
        private RayPointerCollider? _rayCollider;

        public void CapturePointer()
        {
            _isCaptured = true;
        }

        public void ReleasePointer()
        {
            _isCaptured = false;
        }

        public RayPointerStatus GetPointerStatus()
        {
           return _frame.Status;    
        }

        protected override void Update(RenderContext ctx)
        {
            MoveNext();
        }


        [Action()]
        public void MoveNext()
        {
            if (_session?.Frames == null)
                return;

            if (CurrentFrame < 0 || CurrentFrame >= _session.Frames.Count)
                return;

            _rayCollider ??= _host!.Scene?.Component<RayPointerCollider>();

            if (_rayCollider != null && _rayCollider.Pointer != this)
                _rayCollider.Pointer = this;

            _frame = _session.Frames[CurrentFrame];

            CurrentFrame++;
        }

        [Action]
        public void Load()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };

            var path = Context.Require<IPlatform>().PersistentPath;

            var json = File.ReadAllText(Path.Join(path, "pointer.json"));

            _session = JsonSerializer.Deserialize<RayPointerRecorder.RecordSession>(json, options);
            CurrentFrame = 0;
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

        public int PointerId => _session?.PointerId ?? 0;

        public string Name => _session?.PointerName ?? string.Empty;    

        public bool IsCaptured => _isCaptured;

        public int CurrentFrame { get; set; }

        [ValueType(ValueType.FileName)]
        public string? SourceFile { get; set; }  
    }
}
