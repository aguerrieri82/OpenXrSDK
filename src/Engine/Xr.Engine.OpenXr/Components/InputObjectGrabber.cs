using OpenXr.Framework;
using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class InputObjectGrabber : BaseObjectGrabber<Scene>
    {
        protected readonly XrInput<XrPose> _input;
        protected readonly XrInput<float>[] _handlers;

        public InputObjectGrabber(XrInput<XrPose> input, XrHaptic? vibrate, params XrInput<float>[] handlers)
            : base(vibrate)
        {
            _input = input;
            _handlers = handlers;
        }

        protected override bool IsGrabbing(out XrPose? grabPoint)
        {
            grabPoint = _input.Value;
            return _handlers.All(a => a.Value > 0.8); 
        }
    }
}
