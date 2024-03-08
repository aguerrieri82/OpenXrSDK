using OpenXr.Framework;
using Xr.Math;

namespace Xr.Engine.OpenXr
{
    public class InputObjectGrabber : BaseObjectGrabber<Scene>
    {
        protected readonly XrInput<Pose3> _input;
        protected readonly XrInput<float>[] _handlers;

        public InputObjectGrabber(XrInput<Pose3> input, XrHaptic? vibrate, params XrInput<float>[] handlers)
            : base(vibrate)
        {
            _input = input;
            _handlers = handlers;
        }

        protected override ObjectGrab IsGrabbing()
        {
            return new ObjectGrab
            {
                Pose = _input.Value,
                IsGrabbing = _handlers.All(a => a.Value > 0.8),
                IsValid = _input.IsActive
            };
        }
    }
}
