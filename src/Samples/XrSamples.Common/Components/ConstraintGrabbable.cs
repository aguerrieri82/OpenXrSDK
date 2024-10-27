using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Components
{
    public class ConstraintGrabbable : BoundsGrabbable
    {
        public override void NotifyMove()
        {
            _host!.Transform.SetPositionY(_host.Transform.LocalPivot.Y * _host.Transform.Scale.Y);

            var rot = _host!.Transform.Rotation;
            rot.X = 0;
            rot.Z = 0;
            _host.Transform.Rotation = rot;

            base.NotifyMove();
        }
    }
}
