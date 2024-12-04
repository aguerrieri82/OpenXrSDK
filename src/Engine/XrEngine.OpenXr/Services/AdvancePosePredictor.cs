using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.OpenXr
{
    public class AdvancePosePredictor : IPosePredictor
    {
        private Pose3? _prevPose = null;       // Previous pose
        private Pose3? _lastPose = null;       // Last pose
        private float _prevTime = 0f;          // Time of the previous pose
        private float _lastTime = 0f;          // Time of the last pose

        private Vector3 _velocity = Vector3.Zero;            // Current velocity
        private Vector3 _acceleration = Vector3.Zero;        // Current acceleration
        private Vector3 _angularVelocity = Vector3.Zero;     // Current angular velocity
        private Vector3 _angularAcceleration = Vector3.Zero; // Current angular acceleration

        public Pose3 Predict(float dt)
        {
            if (_lastPose == null || _prevPose == null)
                throw new InvalidOperationException("Not enough data to predict.");

            var lastPose = _lastPose.Value;

            // Predict Position: Use velocity and acceleration
            var predictedPosition = lastPose.Position +
                                    _velocity * dt +
                                    0.5f * _acceleration * dt * dt;

            // Predict Orientation: Use angular velocity and acceleration
            var angularDisplacement = _angularVelocity * dt +
                                       0.5f * _angularAcceleration * dt * dt;

            var predictedOrientation = lastPose.Orientation *
                Quaternion.CreateFromAxisAngle(Vector3.Normalize(angularDisplacement), angularDisplacement.Length());

            return new Pose3
            {
                Position = predictedPosition,
                Orientation = predictedOrientation
            };
        }

        public void Track(Pose3 pose, float time)
        {
            // Update the previous and last poses
            _prevPose = _lastPose;
            _prevTime = _lastTime;

            _lastPose = pose;
            _lastTime = time;

            // Calculate velocity and acceleration only if there is enough data
            if (_prevPose.HasValue)
            {
                var deltaTime = _lastTime - _prevTime;
                if (deltaTime > 0)
                {
                    // Linear velocity and acceleration
                    var newVelocity = (_lastPose.Value.Position - _prevPose.Value.Position) / deltaTime;
                    _acceleration = (_velocity == Vector3.Zero) ? Vector3.Zero : (newVelocity - _velocity) / deltaTime;
                    _velocity = newVelocity;

                    // Angular velocity and acceleration
                    var deltaRotation = Quaternion.Conjugate(_prevPose.Value.Orientation) * _lastPose.Value.Orientation;
                    var angularVelocity = QuaternionToAxisAngle(deltaRotation).Axis *
                                          (QuaternionToAxisAngle(deltaRotation).Angle / deltaTime);
                    _angularAcceleration = (_angularVelocity == Vector3.Zero) ? Vector3.Zero : (angularVelocity - _angularVelocity) / deltaTime;
                    _angularVelocity = angularVelocity;
                }
            }
        }

        private (Vector3 Axis, float Angle) QuaternionToAxisAngle(Quaternion q)
        {
            // Ensure quaternion is normalized
            q = Quaternion.Normalize(q);

            // Calculate the angle (acos of w gives half the angle)
            float angle = 2.0f * MathF.Acos(q.W);
            float sinHalfAngle = MathF.Sqrt(1.0f - q.W * q.W);

            // Avoid division by zero for very small angles
            Vector3 axis = sinHalfAngle > 0.001f
                ? new Vector3(q.X, q.Y, q.Z) / sinHalfAngle
                : Vector3.UnitX; // Default axis if no significant rotation

            return (axis, angle);
        }
    }
}
