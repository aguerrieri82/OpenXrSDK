using System.Diagnostics;
using System.Numerics;
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
        private Vector3 _curPivot;
        private Vector3 _lastPivotGlobal;
        private bool _lastKinematic;
        private Vector3 _lastPos;
        private Quaternion _lastOri;
        private Vector3 _lastRefAcc;
        private Vector3 _lastRefSpeed;
        private Quaternion _lastRefOri;
        private Vector3 _lastRefAngSpeed;
        private Vector3 _lastRefPos;
        private PhysicsManager? _manager;
        private double _lastUpdateSpeedTime;

        public void DrawGizmos(Canvas3D canvas)
        {
            var vector = _lastPivotGlobal + (_lastRefSpeed / 2f);
            canvas.Save();
            canvas.State.Color = "#0000FF";
            canvas.State.LineWidth = 2f;
            canvas.DrawLine(_lastPivotGlobal, vector);
            canvas.Restore();
        }

        public static Vector3 CalculateAngularVelocity(Quaternion q1, Quaternion q2, float deltaTime)
        {
            // Step 1: Compute the relative quaternion (difference)
            Quaternion deltaQ = q2 * Quaternion.Inverse(q1);

            // Step 2: Calculate the angle of rotation (in radians)
            float angle = 2.0f * (float)Math.Acos(deltaQ.W);

            // Check for small angle to avoid division by zero
            if (angle < 1e-6)
                return Vector3.Zero;

            // Step 3: Calculate the axis of rotation
            float sinHalfAngle = (float)Math.Sqrt(1.0 - deltaQ.W * deltaQ.W);
            Vector3 axis = new Vector3(deltaQ.X, deltaQ.Y, deltaQ.Z) / sinHalfAngle;

            // Step 4: Calculate angular velocity vector
            Vector3 angularVelocity = (angle / deltaTime) * axis;

            return angularVelocity;
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            var tool = _host.GetActiveTool();

            if (tool != null && _lastTool != tool)
            {
                _lastTool = tool;
                _curPivot = _host.Transform.LocalPivot;
            }

            _lastPivotGlobal = _host.ToWorld(_curPivot);   

            if (!_host.TryComponent<RigidBody>(out var rigidBody))
                return;

            var mustThrow = false;

            if (rigidBody.DynamicActor.IsKinematic != _lastKinematic)
            {
                _lastKinematic = rigidBody.DynamicActor.IsKinematic;

                mustThrow = _lastKinematic == false && _lastTool is InputObjectGrabber;
            }

            var c = SmoothFactor;

            if (!_lastPos.IsFinite())
                _lastPos = Vector3.Zero;
            
            if (!_lastRefSpeed.IsFinite())
                _lastRefSpeed = Vector3.Zero;

            if (!_lastRefAcc.IsFinite())
                _lastRefAcc = Vector3.Zero;

            _manager ??= _host.Scene!.Component<PhysicsManager>();

            var curTime = _manager.Time;    

            var curPos = Vector3.Lerp(_lastPivotGlobal, _lastPos, c);

            var curOri = Quaternion.Slerp(_host.WorldOrientation, _lastOri, c);   

            var dt = curTime - _lastUpdateSpeedTime;

            if (dt > MinDeltaTime)
            {
                var curSpeed = (curPos - _lastRefPos) / (float)dt;
                var curAcc = (curSpeed - _lastRefSpeed) / (float)dt;
                var curAngSpeed = CalculateAngularVelocity(_lastRefOri, curOri, (float)dt);

                _lastRefPos = curPos;
                _lastRefAcc = curAcc;
                _lastRefSpeed = curSpeed;
                _lastRefOri = curOri;
                _lastRefAngSpeed = curAngSpeed;

                _lastUpdateSpeedTime = curTime; 
            }

            _lastPos = curPos;
            _lastOri = curOri;

            if (mustThrow)
            {
                if (!AutoThrow)
                {
                    rigidBody.DynamicActor.LinearVelocity = _lastRefSpeed;
                    rigidBody.DynamicActor.AngularVelocity = _lastRefAngSpeed;
                    rigidBody.DynamicActor.AddForce(_lastRefAcc, _lastPivotGlobal, PhysX.PxForceMode.Acceleration);
                }

                Log.Checkpoint($"Throw: {Math.Round(_lastRefAcc.Length(), 3)}", "#00ff00");
            }

            var velocity = rigidBody.DynamicActor.LinearVelocity;

            Log.Value($"{_host.Name}.Velocity", velocity.Length());

            Log.Value($"{_host.Name}.Acc", _lastRefAcc.Length());

            Log.Value($"{_host.Name}.Velo", _lastRefSpeed.Length());

            Log.Value($"{_host.Name}.Pos", _lastPos.Length());
        }

        public static float MinDeltaTime = 0f;

        public static bool AutoThrow;

        public static float SmoothFactor = 0.3f;

    }
}
