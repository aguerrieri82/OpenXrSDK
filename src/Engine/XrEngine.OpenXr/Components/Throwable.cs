using System.Diagnostics;
using System.Numerics;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
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
            WeightedQuadratic,
            WeightedExponential
        }


        InputGrabber? _lastTool;
        bool _lastKinematic;
        PhysicsManager? _manager;
        RigidBody? _body;
        Vector3 _curPivot;
        double _lastSampleTime;
        readonly List<PoseSample> _poseList;
        float[]? _weights;
        AvgMode _curSampleMode;
        private bool _isMoving;

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
                    if (mode == AvgMode.WeightedExponential)
                        weights[i] = MathF.Pow(2f, i);
                    else
                    {
                        var rawValue = (i + 1);

                        if (mode == AvgMode.WeightedQuadratic)
                            rawValue = rawValue * rawValue;

                        weights[i] = rawValue;

                    }
                    totalSum += weights[i];
                }

                for (var i = 0; i < count; i++)
                    weights[i] /= totalSum;
            }

            return weights;
        }

        static Vector3 CalculateAngularVelocity(in Quaternion from, in Quaternion to, float deltaTime)
        {
            if (deltaTime <= 0f)
                return Vector3.Zero;

            var dq = to * Quaternion.Inverse(from);
            dq = Quaternion.Normalize(dq);

            if (dq.W < 0f)
                dq = new Quaternion(-dq.X, -dq.Y, -dq.Z, -dq.W);

            var w = Math.Clamp(dq.W, -1f, 1f);
            var halfAngle = MathF.Acos(w);
            var sinHalf = MathF.Sin(halfAngle);

            if (sinHalf < 1e-6f)
            {
                var v = new Vector3(dq.X, dq.Y, dq.Z);
                return (2f / deltaTime) * v;
            }

            var axis = new Vector3(dq.X, dq.Y, dq.Z) / sinHalf;
            var angle = 2f * halfAngle;

            return (angle / deltaTime) * axis;
        }

        private Vector3 CompensateForCenterOfMass(Vector3 pivotVelocity, Vector3 angularVelocity, Vector3 worldPivot)
        {
            var localCoM = _body!.DynamicActor.CenterOfMassLocalPose.Position;

            var worldCoM = _host!.ToWorld(localCoM);

            var radiusVector = worldCoM - worldPivot;

            var tangentialVelocity = Vector3.Cross(angularVelocity, radiusVector);

            return pivotVelocity + tangentialVelocity;
        }

        protected override void Update(RenderContext ctx)
        {
            if (AutoThrow)
                return;

            Debug.Assert(_host != null);

            var tool = _host.GetActiveTool();

            if (tool == null && !_isMoving)
                return;

            if (tool != null && _lastTool != tool)
            {
                if (tool is not InputGrabber grabber)
                    return;

                _lastTool = grabber;
                _curPivot = _host.Transform.LocalPivot;
                _isMoving = true;
            }


            _manager ??= _host.Scene!.Component<PhysicsManager>();

            _body ??= _host.Component<RigidBody>();

            _body.TrackVelocityOnTool = AutoThrow;

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

            if (!UseInput && (_lastKinematic || mustThrow))
            {
                var deltaTime = curTime - _lastSampleTime;

                if ((deltaTime > MinDeltaTime || mustThrow) && deltaTime > 0)
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

            if (mustThrow)
            {

                var velocity = Vector3.Zero;
                var angVel = Vector3.Zero;
                Vector3 lastWordPos;

                if (UseInput && _lastTool?.Input?.LinearVelocity != null && _lastTool.Input?.AngularVelocity != null)
                {
                    Log.Info(this, "-----THROW INPUT-----");
                    velocity = _lastTool.Input.LinearVelocity.Value;
                    angVel = _lastTool.Input.AngularVelocity.Value;
                    lastWordPos = _lastTool.Input.Value.Position;
                }
                else
                {
                    var pointCount = _poseList.Count - SamplesToSkip - 1;

                    if (pointCount < 2)
                        return;

                    if (_weights == null || _weights.Length != pointCount || SamplingMode != _curSampleMode)
                    {
                        _weights = GenerateWeights(pointCount, SamplingMode);
                        _curSampleMode = SamplingMode;
                    }

                    for (var i = 0; i < pointCount; i++)
                    {
                        var dt = (float)(_poseList[i + 1].Time - _poseList[i].Time);
                        var dv = _poseList[i + 1].Pose.Position - _poseList[i].Pose.Position;
                        var curVel = dv / dt;
                        var curVelAng = CalculateAngularVelocity(
                             _poseList[i].Pose.Orientation,
                             _poseList[i + 1].Pose.Orientation,
                             dt);

                        velocity += curVel * _weights[i];
                        angVel += curVelAng * _weights[i];
                    }

                    lastWordPos = _poseList[_poseList.Count - 1].Pose.Position;

                }
                var finalVelocity = CompensateForCenterOfMass(
                    velocity,
                    angVel,
                    lastWordPos
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

                _lastTool = null;
                _isMoving = false;
            }
        }


        public float MinDeltaTime { get; set; }

        public bool AutoThrow { get; set; }

        public int MaxSamples { get; set; }

        public int SamplesToSkip { get; set; }

        public float Amplification { get; set; }

        public AvgMode SamplingMode { get; set; }

        public bool UseInput { get; set; }
    }

}
