using Silk.NET.OpenXR;
using Action = Silk.NET.OpenXR.Action;

namespace OpenXr.Framework
{
    public class XrHaptic : IXrAction
    {
        protected Action _action;
        protected ulong _subPath;
        protected readonly XrApp _app;
        protected readonly string _name;
        protected readonly string _path;

        public XrHaptic(XrApp app, string path, string name)
        {
            _app = app;
            _path = path;
            _name = name;
        }

        public virtual ActionSuggestedBinding Initialize()
        {
            var result = new ActionSuggestedBinding
            {
                Binding = _app.StringToPath(_path),
                Action = _action.Handle == 0 ? _app.CreateAction(_name, _name, ActionType.VibrationOutput) : _action
            };
            _action = result.Action;
            return result;
        }

        public void VibrateStart(float frequencyHz, float amplitude, TimeSpan duration)
        {
            _app.ApplyVibrationFeedback(_action, frequencyHz, amplitude, duration, _subPath);
        }

        public void VibrateStop()
        {
            _app.StopHapticFeedback(_action);
        }

        public Action Action => _action;

        public string Name => _name;

    }
}
