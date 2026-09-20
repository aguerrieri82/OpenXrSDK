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
            MaxSamples = 5;
            MinDeltaTime = 0.025f;
            SamplesToSkip = 0;
            Amplification = 2.5f;
            SamplingMode = AvgMode.Weighted;
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

        static Vector3 CalculateLinearVelocity(List<PoseSample> samples, int count, float[] weights)
        {
            var refTime = samples[count - 1].Time;

            var meanTime = 0d;
            var meanPosition = Vector3.Zero;

            for (var i = 0; i < count; i++)
            {
                var time = samples[i].Time - refTime;

                meanTime += time * weights[i];
                meanPosition += samples[i].Pose.Position * weights[i];
            }

            var numerator = Vector3.Zero;
            var denominator = 0d;

            for (var i = 0; i < count; i++)
            {
                var time = samples[i].Time - refTime;
                var dt = time - meanTime;

                numerator += (samples[i].Pose.Position - meanPosition) * (float)(weights[i] * dt);
                denominator += weights[i] * dt * dt;
            }

            if (denominator < 1e-8)
                return Vector3.Zero;

            return numerator / (float)denominator;
        }

        static Vector3 CalculateAngularVelocity(List<PoseSample> samples, int count, float[] weights)
        {
            var refTime = samples[count - 1].Time;
            var refOrientation = samples[count - 1].Pose.Orientation;

            var meanTime = 0d;
            var meanRotation = Vector3.Zero;

            for (var i = 0; i < count; i++)
            {
                var time = samples[i].Time - refTime;
                var rotation = CalculateRotationVector(refOrientation, samples[i].Pose.Orientation);

                meanTime += time * weights[i];
                meanRotation += rotation * weights[i];
            }

            var numerator = Vector3.Zero;
            var denominator = 0d;

            for (var i = 0; i < count; i++)
            {
                var time = samples[i].Time - refTime;
                var dt = time - meanTime;
                var rotation = CalculateRotationVector(refOrientation, samples[i].Pose.Orientation);

                numerator += (rotation - meanRotation) * (float)(weights[i] * dt);
                denominator += weights[i] * dt * dt;
            }

            if (denominator < 1e-8)
                return Vector3.Zero;

            return numerator / (float)denominator;
        }

        static Vector3 CalculateRotationVector(in Quaternion from, in Quaternion to)
        {
            var dq = to * Quaternion.Inverse(from);
            dq = Quaternion.Normalize(dq);

            if (dq.W < 0f)
                dq = new Quaternion(-dq.X, -dq.Y, -dq.Z, -dq.W);

            var w = Math.Clamp(dq.W, -1f, 1f);
            var halfAngle = MathF.Acos(w);
            var sinHalf = MathF.Sin(halfAngle);

            if (sinHalf < 1e-6f)
                return new Vector3(dq.X, dq.Y, dq.Z) * 2f;

            var axis = new Vector3(dq.X, dq.Y, dq.Z) / sinHalf;
            var angle = 2f * halfAngle;

            return axis * angle;
        }

        private Vector3 CompensateForCenterOfMass(Vector3 pivotVelocity, Vector3 angularVelocity, Vector3 worldPivot)
        {
            var localCoM = _body!.DynamicActor.CenterOfMassLocalPose.Position;

            var worldCoM = _host.ToWorld(localCoM);

            var radiusVector = worldCoM - worldPivot;

            var tangentialVelocity = Vector3.Cross(angularVelocity, radiusVector);

            return pivotVelocity + tangentialVelocity;
        }

        private void EndThrow()
        {
            _lastTool = null;
            _isMoving = false;
            _poseList.Clear();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            _manager ??= _host.Scene!.Component<PhysicsManager>();

            _body ??= _host.Component<RigidBody>();

            _body.TrackVelocityOnTool = TrackVelocity;

            if (AutoThrow)
                return;

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
                Vector3 lastWorldPos;

                if (UseInput && _lastTool?.Input?.LinearVelocity != null && _lastTool.Input?.AngularVelocity != null)
                {
                    Log.Info(this, "-----THROW INPUT-----");
                    velocity = _lastTool.Input.LinearVelocity.Value;
                    angVel = _lastTool.Input.AngularVelocity.Value;
                    lastWorldPos = _lastTool.Input.Value.Position;
                }
                else
                {
                    var sampleCount = _poseList.Count - SamplesToSkip;

                    if (sampleCount < 2)
                    {
                        EndThrow();
                        return;
                    }

                    if (_weights == null || _weights.Length != sampleCount || SamplingMode != _curSampleMode)
                    {
                        _weights = GenerateWeights(sampleCount, SamplingMode);
                        _curSampleMode = SamplingMode;
                    }

                    velocity = CalculateLinearVelocity(_poseList, sampleCount, _weights);
                    angVel = CalculateAngularVelocity(_poseList, sampleCount, _weights);

                    lastWorldPos = _poseList[_poseList.Count - 1].Pose.Position;
                }

                var finalVelocity = CompensateForCenterOfMass(
                    velocity,
                    angVel,
                    lastWorldPos
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

                EndThrow();
            }
        }

        public float MinDeltaTime { get; set; }

        public bool AutoThrow { get; set; }

        public int MaxSamples { get; set; }

        public int SamplesToSkip { get; set; }

        public float Amplification { get; set; }

        public AvgMode SamplingMode { get; set; }

        public bool UseInput { get; set; }

        public bool TrackVelocity { get; set; }
    }

}