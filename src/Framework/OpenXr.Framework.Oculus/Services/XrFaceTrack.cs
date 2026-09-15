using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;

namespace OpenXr.Framework.Oculus
{
    public struct XrFaceWeight
    {
        public FaceExpression2FB Type;

        public float Weight;

        public float Confidence;
    }

    public class XrFaceTrack : IDisposable
    {
        protected readonly XrApp _app;
        protected FBFaceTracking2? _faceTraking;
        protected FaceTracker2FB _tracker;
        protected XrFaceWeight[]? _weights;
        protected bool _isValid;
        protected FaceTrackingDataSource2FB _source;
        protected long _time;

        public XrFaceTrack(XrApp app)
        {
            _app = app;
        }

        protected void Initialize()
        {
            if (_faceTraking != null)
                return;

            if (!_app.Xr.TryGetInstanceExtension<FBFaceTracking2>(null, _app.Instance, out _faceTraking))
                throw new NotSupportedException();
        }

        public bool IsSupported()
        {
            var bodyProps = new SystemFaceTrackingProperties2FB
            {
                Type = StructureType.SystemFaceTrackingProperties2FB
            };

            _app.GetSystemProperties(ref bodyProps);

            return bodyProps.SupportsAudioFaceTracking != 0 || bodyProps.SupportsVisualFaceTracking != 0;
        }

        public unsafe void Create(FaceExpressionSet2FB expressionSet = FaceExpressionSet2FB.DefaultFB)
        {
            Initialize();

            if (!IsSupported())
                throw new Exception("Face tracking not supported");

            var dataSources = stackalloc FaceTrackingDataSource2FB[]
            {
                FaceTrackingDataSource2FB.VisualFB,
                FaceTrackingDataSource2FB.AudioFB
            };

            var info = new FaceTrackerCreateInfo2FB()
            {
                Type = StructureType.FaceTrackerCreateInfo2FB,
                FaceExpressionSet = expressionSet,
                RequestedDataSourceCount = 2,
                RequestedDataSources = dataSources
            };

            var result = new FaceTracker2FB();

            _app.CheckResult(_faceTraking!.CreateFaceTracker2fB(_app.Session, ref info, ref result), "CreateFaceTracker2fB");

            _tracker = result;
        }

        public unsafe XrFaceWeight[]? GetWeigths(long time = 0)
        {
            var info = new FaceExpressionInfo2FB()
            {
                Type = StructureType.FaceExpressionInfo2FB,
                Time = time == 0 ? _app.FramePredictedDisplayTime : time,
            };

            var weights = new float[(int)FaceExpression2FB.CountFB];
            var confs = new float[(int)FaceConfidence2FB.CountFB];

            fixed (float* pWeights = &weights[0])
            fixed (float* pConf = &confs[0])
            {
                var result = new FaceExpressionWeights2FB
                {
                    Type = StructureType.FaceExpressionWeights2FB,
                    Confidences = pConf,
                    ConfidenceCount = (uint)confs.Length,
                    Weights = pWeights,
                    WeightCount = (uint)weights.Length
                };

                _app.CheckResult(_faceTraking!.GetFaceExpressionWeights2fB(_tracker, ref info, ref result), "GetFaceExpressionWeights2fB");

                _isValid = result.IsValid != 0;
                _source = result.DataSource;
                _time = result.Time;

                if (_isValid)
                {
                    _weights = new XrFaceWeight[weights.Length];
                    for (var i = 0; i < weights.Length; i++)
                    {
                        _weights[i] = new XrFaceWeight
                        {
                            Weight = weights[i],
                            //Confidence = confs[i],
                            Type = (FaceExpression2FB)i
                        };
                    }
                }
            }

            return _weights;

        }

        public void Destroy()
        {
            if (_tracker.Handle == 0)
                return;

            if (_app.IsStarted)
                _app.CheckResult(_faceTraking!.DestroyFaceTracker2fB(_tracker), "DestroyFaceTracker2fB");

            _tracker.Handle = 0;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public bool IsValid => _isValid;

        public FaceTrackingDataSource2FB DataSource => _source;

        public long Time => _time;

        public XrFaceWeight[]? Weights => _weights;
    }
}
