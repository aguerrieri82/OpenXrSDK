using CanvasUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;
using IDrawGizmos = XrEngine.IDrawGizmos;

namespace XrSamples
{
    public class SpeedTracker : Behavior<Object3D>, IDrawGizmos
    {
        IObjectTool? _lastTool;
        private Vector3 _lastPivotGlobal;
        private bool _lastKinematic;
        private Vector3 _lastPos;
        private Vector3 _lastAcc;
        private Vector3 _lastSpeed;

        public void DrawGizmos(Canvas3D canvas)
        {
            var vector = _lastPivotGlobal + (_lastSpeed / 2f);
            canvas.Save();
            canvas.State.Color = "#0000FF";
            canvas.State.LineWidth = 2f;
            canvas.DrawLine(_lastPivotGlobal, vector);
            canvas.Restore();
        }

        protected override void Update(RenderContext ctx)
        {
            var tool = _host!.GetActiveTool();  

            if (tool != null)
            {
                _lastTool = tool;
                _lastPivotGlobal = _host!.Transform.LocalPivot.Transform(_host.WorldMatrix);  
            }
  

            if (!_host!.TryComponent<RigidBody>(out var rigidBody))
                return;

            var mustThrow = false;

            if (rigidBody.DynamicActor.IsKinematic != _lastKinematic)
            {
                _lastKinematic = rigidBody.DynamicActor.IsKinematic;

                mustThrow = _lastKinematic == false && _lastTool is InputObjectGrabber;
            }

            var c = SmoothFactor;

            Vector3 Smooth(Vector3 value, Vector3 lastValue)
            {
                return (lastValue * (1 - c)) + (value * c);
            }

            var curPos = Smooth(_host!.WorldPosition, _lastPos);
            var curSpeed = Smooth((curPos - _lastPos) / (float)ctx.DeltaTime, _lastSpeed);
            curSpeed = rigidBody.DynamicActor.LinearVelocity;
            var curAcc = Smooth((curSpeed - _lastSpeed) / (float)ctx.DeltaTime, _lastAcc);



            if (mustThrow)
            {
                //rigidBody.DynamicActor.Stop();
                rigidBody.DynamicActor.IsKinematic = false;
                var force = curAcc * (float)ctx.DeltaTime * rigidBody.DynamicActor.Mass;
               // rigidBody.DynamicActor.AddForce(force, _lastPivotGlobal, PhysX.PxForceMode.Impulse);
                Log.Checkpoint($"Throw: {Math.Round(force.Length(), 3)}", "#00ff00");
            }

            _lastAcc = curAcc;
            _lastSpeed = curSpeed;
            _lastPos = curPos;

            var velocity = rigidBody.DynamicActor.LinearVelocity;

            if (velocity.Length() != 0)
                Log.Value($"{_host!.Name}.Velocity", new Vector2(velocity.X, velocity.Z).Length()); 

            Log.Value($"{_host!.Name}.Acc", curAcc.Length());
        }


        public static float SmoothFactor = 0.3f;

    }
}
