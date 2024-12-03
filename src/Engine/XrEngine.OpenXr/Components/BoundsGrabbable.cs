using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class BoundsGrabbable : Behavior<Object3D>, IGrabbable
    {
        private ILocalBounds? _local;

        protected override void OnAttach()
        {
            _local = _host?.Feature<ILocalBounds>();
            if (_local != null)
                _local.BoundUpdateMode = UpdateMode.Automatic;
        }

        public bool CanGrab(Vector3 position)
        {
            if (_local != null)
                return _local.LocalBounds.Contains(_host!.ToLocal(position));

            return false;
        }

        public virtual void Grab(string grabber)
        {
            Grabber = grabber;  
        }

        public virtual void Release()
        {
            Grabber = null;
        }

        public virtual void NotifyMove()
        {

        }

        public string? Grabber { get; protected set; }
    }
}
