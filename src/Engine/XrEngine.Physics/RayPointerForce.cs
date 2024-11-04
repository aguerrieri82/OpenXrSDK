using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Interaction;
using XrInteraction;
using XrMath;

namespace XrEngine.Physics
{
    public class RayPointerForce : Behavior<Scene3D>, IRayTarget
    {
        Collision? _lastCollision;
        private bool _isDragging;
        private Vector3 _startDragLocal;
        private Object3D? _object;
        private RigidBody? _rigidBody;

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(_host?.Scene != null);

            if (Pointer == null && !string.IsNullOrWhiteSpace(PointerName))
            {
                Pointer = _host!.Scene
                      .Components<IRayPointer>()
                      .Where(a => a.Name == PointerName)
                      .FirstOrDefault();
            }

        }

        protected override void Update(RenderContext ctx)
        {
            if (Pointer == null)
                return;
            var status = Pointer.GetPointerStatus();
            if (!status.IsActive)
                return;

            if (!_isDragging)
            {

                if (_lastCollision?.Object == null)
                    return;

                if (!_lastCollision.Object.TryComponent<RigidBody>(out _rigidBody))
                    return;

                if (status.Buttons == Pointer2Button.Left)
                {
                    _isDragging = true;
                    _startDragLocal = _lastCollision.LocalPoint;
                    _object = _lastCollision.Object;
                }
            }
            else
            {


                if (status.Buttons != Pointer2Button.Left)
                    _isDragging = false;
                else
                {
                    if (_lastCollision == null)
                        return;

                    Debug.Assert(_object != null && _rigidBody != null);

                    var startWorld = _object.ToWorld(_startDragLocal);
                    var curWorlds = _lastCollision.Point;
                    var length = Vector3.Distance(startWorld, curWorlds);
                    var dir = (curWorlds - startWorld).Normalize();
                    _rigidBody.DynamicActor.AddForce(dir, _startDragLocal, PhysX.PxForceMode.Force);
                    
                }
            }
            base.Update(ctx);
        }

        public void NotifyCollision(RenderContext ctx, Collision? collision)
        {
            _lastCollision = collision;
        }

        public IRayPointer? Pointer { get; set; }

        public string? PointerName { get; set; }

    }
}
