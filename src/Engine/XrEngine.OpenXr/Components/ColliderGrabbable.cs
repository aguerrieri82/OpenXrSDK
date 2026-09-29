
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrEngine.OpenXr
{
    public class ColliderGrabbable : Behavior<Object3D>, IGrabbable
    {
        ICollider3D[] _colliders = [];

        protected override void OnAttach()
        {
            _colliders = _host?.Components<ICollider3D>().ToArray() ?? [];
        }

        public bool CanGrab(Vector3 position)
        {
            if (!IsEnabled)
                return false;

            foreach (var collider in _colliders)
            {
                if (collider.IsEnabled && collider.ContainsPoint(position))
                    return true;
            }

            return false;
        }

        public void Grab(string grabber)
        {
            Grabber = grabber;
        }

        public void Release()
        {
            Grabber = null;
        }

        public void NotifyMove()
        {
        }

        public string? Grabber { get; protected set; }
    }
}