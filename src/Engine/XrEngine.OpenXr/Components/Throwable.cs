using System.Diagnostics;
using System.Numerics;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr.Components
{

    public class Throwable : Behavior<Object3D>
    {
        public struct PoseSample
        {
            public Pose3 Pose;

            public double Time;
        }

        public enum AvgMode
        {
            Normal = 0,
            Weighted,
            WeightedExponential
        }


        IObjectTool? _lastTool;
        bool _lastKinematic;
        PhysicsManager? _manager;
        RigidBody? _body;
        Vector3 _curPivot;
        double _lastSampleTime;
        readonly List<PoseSample> _poseList;
        float[]? _weights;
        AvgMode _curSampMode;

        public Throwable()
        {
            _poseList = [];
            MaxSamples = 4;
            MinDeltaTime = 0.020f;
            SamplesToSkip = 0;
            Amplification = 1.5f;
            SamplingMode = AvgMode.WeightedExponential;
        }

        static float[] GenerateWeights(int count, AvgMode mode)
        {
            var weights = new float[count];
            if (mode == AvgMode.Normal)
            {
                for (var i = 0; i < count; i++)
                    weights[i] = 1f / count;
            }

            else
            {
                var totalSum = 0f;

                for (var i = 0; i < count; i++)
                {
                    var rawValue = (i + 1);

                    if (mode == AvgMode.WeightedExponential)
                        rawValue = rawValue * rawValue;

                    weights[i] = rawValue;
                    totalSum += rawValue;
                }

                for (var i = 0; i < count; i++)
                    weights[i] /= totalSum;
            }

            return weights;
        }

        static Vector3 CalculateAngularVelocity(Quaternion q1, Quaternion q2, float deltaTime)
        {
            var deltaQ = q2 * Quaternion.Inverse(q1);

            var angle = 2.0f * MathF.Acos(deltaQ.W);

            if (angle < 1e-6)
                return Vector3.Zero;

            var sinHalfAngle = MathF.Sqrt(1.0f - deltaQ.W * deltaQ.W);

            var axis = new Vector3(deltaQ.X, deltaQ.Y, deltaQ.Z) / sinHalfAngle;

            return (angle / deltaTime) * axis;
        }

        private Vector3 CompensateForCenterOfMass(Vector3 pivotVelocity, Vector3 angularVelocity, Vector3 worldPivot, RigidBody rigidBody)
        {
            var localCoM = rigidBody.DynamicActor.CenterOfMassLocalPose.Position;

            var worldCoM = _host!.ToWorld(localCoM);

            var radiusVector = worldCoM - worldPivot;

            var tangentialVelocity = Vector3.Cross(angularVelocity, radiusVector);

            return pivotVelocity + tangentialVelocity;
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            _manager ??= _host.Scene!.Component<PhysicsManager>();

            _body ??= _host.Component<RigidBody>();

            _body.TrackVelocityOnTool = AutoThrow;

            var tool = _host.GetActiveTool();

            if (tool != null && _lastTool != tool)
            {
                _lastTool = tool;
                _curPivot = _host.Transform.LocalPivot;
            }

            if (_lastTool != null && _lastTool is not InputGrabber)
                return;

            var mustThrow = false;

            var curTime = ctx.Time;

            if (_body.DynamicActor.IsKinematic != _lastKinematic)
            {
                _lastKinematic = _body.DynamicActor.IsKinematic;

                if (_lastKinematic)
                {
                    _poseList.Clear();
                    _lastSampleTime = curTime;
                }
                else
                    mustThrow = true;
            }

            if (_lastKinematic || mustThrow)
            {
                var deltaTime = curTime - _lastSampleTime;

                if (deltaTime > MinDeltaTime && deltaTime > 0)
                {
                    _poseList.Add(new PoseSample
                    {
                        Pose = new Pose3
                        {
                            Orientation = _host.WorldOrientation,
                            Position = _host.ToWorld(_curPivot)
                        },
                        Time = curTime,
                    });

                    while (_poseList.Count > MaxSamples)
                        _poseList.RemoveAt(0);

                    _lastSampleTime = curTime;
                }
            }

            if (mustThrow && !AutoThrow)
            {
                var velocity = Vector3.Zero;
                var angVel = Vector3.Zero;

                var pointCount = _poseList.Count - SamplesToSkip - 1;

                if (pointCount < 2)
                    return;

                if (_weights == null || _weights.Length != pointCount || SamplingMode != _curSampMode)
                {
                    _weights = GenerateWeights(pointCount, SamplingMode);
                    _curSampMode = SamplingMode;
                }

                for (var i = 0; i < pointCount; i++)
                {
                    var dt = (float)(_poseList[i + 1].Time - _poseList[i].Time);
                    var dv = _poseList[i + 1].Pose.Position - _poseList[i].Pose.Position;
                    var curVel = dv / dt;
                    var curVelAng = CalculateAngularVelocity(_poseList[i + 1].Pose.Orientation, _poseList[i].Pose.Orientation, dt);

                    velocity += curVel * _weights[i];
                    angVel += curVelAng * _weights[i];
                }

                var finalVelocity = CompensateForCenterOfMass(
                    velocity,
                    angVel,
                    _poseList[_poseList.Count - 1].Pose.Position,
                    _body
                );

                finalVelocity *= Amplification;

                Log.Info(this, "-----THROW-----");
                Log.Info(this, "Computed Vel: " + velocity);
                Log.Info(this, "Computed Vel F: " + finalVelocity);
                Log.Info(this, "Cur Vel: " + _body.DynamicActor.LinearVelocity);
                Log.Info(this, "Computed Ang Vel: " + angVel);
                Log.Info(this, "Cur Ang Vel: " + _body.DynamicActor.AngularVelocity);

                _body.DynamicActor.LinearVelocity = finalVelocity;
                _body.DynamicActor.AngularVelocity = angVel;

            }
        }


        public float MinDeltaTime { get; set; }

        public bool AutoThrow { get; set; }

        public float Correction { get; set; }

        public int MaxSamples { get; set; }

        public int SamplesToSkip { get; set; }

        public float Amplification { get; set; }

        public AvgMode SamplingMode { get; set; }
    }

}
