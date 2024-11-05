using OpenXr.Framework;
using System;
using System.Collections.Concurrent;
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
    public class InputObjectForce : Behavior<Scene3D>, IDrawGizmos
    {
        private bool _isDragging;
        private Vector3 _startDragLocal;
        private Object3D? _object;
        private RigidBody? _rigidBody;
        private Line3 _lastForce;
        private readonly ConcurrentBag<Object3D> _checkObjects = [];

        public InputObjectForce()
        {
            Factor = 20;
        }

        protected override void Update(RenderContext ctx)
        {
          
            if (Input == null)
            {
                if (XrApp.Current == null || !XrApp.Current.IsStarted)
                    return;

                if (string.IsNullOrWhiteSpace(InputName))
                    return;
                
                Input = XrApp.Current!.Inputs[InputName] as XrPoseInput;
            }

            if (Haptic == null)
            {
                if (XrApp.Current == null || !XrApp.Current.IsStarted)
                    return;

                if (string.IsNullOrWhiteSpace(HapticName))
                    return;

                Haptic = XrApp.Current!.Haptics[HapticName];
            }

            if (Handler == null)
            {
                if (XrApp.Current == null || !XrApp.Current.IsStarted)
                    return;

                if (string.IsNullOrWhiteSpace(HandlerName))
                    return;

                Handler = XrApp.Current!.Inputs[HandlerName] as XrBoolInput;
            }

            if (Input == null || !Input.IsActive || Handler == null)
                return;

            if (!_isDragging)
            {
                if (Handler.IsActive && Handler.Value)
                {
                    _host!.Scene!.ContainsPoint(Input.Value.Position, _checkObjects);

                    foreach (var obj in _checkObjects)
                    {
                        if (!obj.TryComponent<RigidBody>(out _rigidBody))
                            continue;

                        if (_rigidBody.Type == PhysX.Framework.PhysicsActorType.Static)
                            continue;

                        _isDragging = true;
                        _object = obj;
                        _startDragLocal = _object.ToLocal(Input.Value.Position);

                        Haptic?.VibrateStart(400, 1, TimeSpan.FromMilliseconds(100));

                        Log.Checkpoint("Force Start", "#00ff00");

                        break;
                    }
                }
            }
            else
            {
                if (Handler.IsActive && Handler.IsChanged && !Handler.Value)
                {
                    Log.Checkpoint("Force End", "#ff0000");
                    _isDragging = false;
                }
              
                else
                {
                    Debug.Assert(_object != null && _rigidBody != null);

                    var startWorld = _object.ToWorld(_startDragLocal);
                    var curWorlds = Input.Value.Position;
                    var force = (curWorlds - startWorld) * Factor * _rigidBody.DynamicActor.Mass;
                    
                    _rigidBody.DynamicActor.AddForce(force, startWorld, PhysX.PxForceMode.Impulse);

                    _lastForce = new Line3() { From = startWorld, To = curWorlds };

                    //_startDragLocal = _host!.ToLocal(curWorlds);
                }
            }
            base.Update(ctx);
        }



        public void DrawGizmos(Canvas3D canvas)
        {
            if (!_isDragging)
                return;
            canvas.Save();
            canvas.State.Color = "#0000FF";
            canvas.DrawLine(_lastForce.From, _lastForce.To);
            canvas.Restore();
        }

        public float Factor { get; set; }

        public XrHaptic? Haptic { get; set; }

        public XrBoolInput? Handler { get; set; }

        public XrPoseInput? Input { get; set; }

        public string? InputName { get; set; }

        public string? HandlerName { get; set; }

        public string? HapticName { get; set; }
    }
}
