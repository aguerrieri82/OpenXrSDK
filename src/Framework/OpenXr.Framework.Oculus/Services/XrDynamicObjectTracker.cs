using Common.Interop;
using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public class XrDynamicObjectTracker : IDisposable
    {
        readonly XrApp _app;
        readonly METADynamicObjectTracker _ext;

        DynamicObjectTrackerMETA _tracker;

        TaskCompletionSource<Result>? _createCompletion;
        TaskCompletionSource<Result>? _setClassesCompletion;

        public XrDynamicObjectTracker(XrApp app)
        {
            _app = app;
            _ext = new METADynamicObjectTracker(app.Xr, app.Instance);

            _app.XrEvent += OnEvent;
        }

        public bool IsSupported()
        {
            var props = new SystemDynamicObjectTrackerPropertiesMETA();
            _app.GetSystemProperties(ref props);

            return props.SupportsDynamicObjectTracker != 0;
        }

        public bool IsKeyboardSupported()
        {
            if (!_app.HasExtension(METADynamicObjectKeyboard.ExtensionName))
                return false;

            var props = new SystemDynamicObjectKeyboardPropertiesMETA();
            _app.GetSystemProperties(ref props);

            return props.SupportsDynamicObjectKeyboard != 0;
        }

        public async Task CreateAsync()
        {
            if (!IsSupported())
                throw new NotSupportedException();

            if (_tracker.Handle != 0)
                return;

            if (_createCompletion != null)
                throw new InvalidOperationException("Dynamic object tracker creation is already pending");

            _createCompletion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);

            var info = new DynamicObjectTrackerCreateInfoMETA();
            _app.CheckResult(_ext.CreateDynamicObjectTrackerMETA(_app.Session, ref info, ref _tracker), "CreateDynamicObjectTrackerMETA");

            var result = await _createCompletion.Task;
            _createCompletion = null;

            _app.CheckResult(result, "CreateDynamicObjectTrackerMETA");
        }

        public async Task SetTrackedClassesAsync(params DynamicObjectClassMETA[] classes)
        {
            if (_tracker.Handle == 0)
                throw new InvalidOperationException("Dynamic object tracker is not created");

            if (_setClassesCompletion != null)
                throw new InvalidOperationException("Set tracked classes is already pending");

            _setClassesCompletion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);

            unsafe
            {
                fixed (DynamicObjectClassMETA* pClasses = classes)
                {
                    var info = new DynamicObjectTrackedClassesSetInfoMETA
                    {
                        ClassCount = (uint)classes.Length,
                        Classes = pClasses
                    };

                    _app.CheckResult(_ext.SetDynamicObjectTrackedClassesMETA(_tracker, ref info), "SetDynamicObjectTrackedClassesMETA");
                }
            }

            var result = await _setClassesCompletion.Task;
            _setClassesCompletion = null;

            _app.CheckResult(result, "SetDynamicObjectTrackedClassesMETA");
        }

        public DynamicObjectClassMETA GetDynamicObjectClass(Space space)
        {
            var data = new DynamicObjectDataMETA();

            _app.CheckResult(_ext.GetSpaceDynamicObjectDataMETA(space, ref data), "GetSpaceDynamicObjectDataMETA");

            return data.ClassType;
        }

        protected void OnEvent(ref EventDataBuffer buffer)
        {
            if (buffer.Type == METADynamicObjectTracker.TypeEventDataDynamicObjectTrackerCreateResultMeta)
            {
                var data = buffer.Convert().To<EventDataDynamicObjectTrackerCreateResultMETA>();

                if (data.Handle.Handle != _tracker.Handle)
                    return;

                _createCompletion?.TrySetResult(data.Result);
            }
            else if (buffer.Type == METADynamicObjectTracker.TypeEventDataDynamicObjectSetTrackedClassesResultMeta)
            {
                var data = buffer.Convert().To<EventDataDynamicObjectSetTrackedClassesResultMETA>();

                if (data.Handle.Handle != _tracker.Handle)
                    return;

                _setClassesCompletion?.TrySetResult(data.Result);
            }
        }

        public void Destroy()
        {
            if (_tracker.Handle == 0)
                return;

            _app.CheckResult(_ext.DestroyDynamicObjectTrackerMETA(_tracker), "DestroyDynamicObjectTrackerMETA");
            _tracker.Handle = 0;
        }

        public void Dispose()
        {
            _app.XrEvent -= OnEvent;

            Destroy();

            _createCompletion = null;
            _setClassesCompletion = null;

            GC.SuppressFinalize(this);
        }

        public bool IsCreated => _tracker.Handle != 0;

        public static implicit operator DynamicObjectTrackerMETA(XrDynamicObjectTracker value)
        {
            return value._tracker;
        }
    }
}