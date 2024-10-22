using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrPassthroughMesh
    {
        public GeometryInstanceFB Instance;

        public TriangleMeshFB Mesh;

        public object? Tag;
    }

    public class XrPassthroughLayer : XrBaseLayer<CompositionLayerPassthroughFB>
    {
        private FBPassthrough? _passthrough;
        private PassthroughFB _ptInstance;
        private PassthroughLayerFB _ptLayer;
        private bool _isStarted;
        private readonly List<XrPassthroughMesh> _meshes = [];
        private XrEnvironmentDepth _envDepth;
        private EnvironmentDepthImageMETA? _depthImage;
        private bool _removeHand;

        public XrPassthroughLayer()
        {
            _envDepth = new XrEnvironmentDepth();
            Purpose = PassthroughLayerPurposeFB.ReconstructionFB;
            Priority = 0;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add(FBPassthrough.ExtensionName);
            extensions.Add(FBPassthroughKeyboardHands.ExtensionName);
            extensions.Add(METAEnvironmentDepth.ExtensionName);

            base.Initialize(app, extensions);
        }

        public override void OnBeginFrame(Space space, long displayTime)
        {
            if (UseEnvironmentDepth)
                _depthImage = _envDepth.Acquire(space, displayTime);

        }

        protected unsafe SystemPassthroughProperties2FB GetPtCapabilities()
        {
            var props = new SystemPassthroughProperties2FB
            {
                Type = StructureType.SystemPassthroughProperties2FB
            };

            _xrApp!.GetSystemProperties(ref props);

            return props;
        }

        protected PassthroughFB CreatePt(PassthroughFlagsFB flags)
        {
            var info = new PassthroughCreateInfoFB
            {
                Type = StructureType.PassthroughCreateInfoFB,
                Flags = flags,
            };

            _xrApp!.CheckResult(_passthrough!.CreatePassthroughFB(_xrApp!.Session, in info, ref _ptInstance), "CreatePassthroughFB");

            return _ptInstance;
        }

        protected PassthroughLayerFB CreatePtLayer(PassthroughLayerPurposeFB purpose, PassthroughFlagsFB flags)
        {
            var info = new PassthroughLayerCreateInfoFB
            {
                Type = StructureType.PassthroughLayerCreateInfoFB,
                Passthrough = _ptInstance,
                Purpose = purpose,
                Flags = flags
            };

            _xrApp!.CheckResult(_passthrough!.CreatePassthroughLayerFB(_xrApp!.Session, in info, ref _ptLayer), "CreatePassthroughLayerFB");

            return _ptLayer;
        }

        protected void StartPt()
        {
            _xrApp!.CheckResult(_passthrough!.PassthroughStartFB(_ptInstance), "PassthroughStartFB");
            _isStarted = true;
        }

        protected void PausePt()
        {
            _xrApp!.CheckResult(_passthrough!.PassthroughPauseFB(_ptInstance), "PassthroughPauseFB");
            _isStarted = false;
        }

        public override void Destroy()
        {
            if (_passthrough != null)
            {
                foreach (var mesh in _meshes)
                    _xrApp!.CheckResult(_passthrough.DestroyGeometryInstanceFB(mesh.Instance), "DestroyGeometryInstanceFB");

                if (_ptLayer.Handle != 0)
                    _xrApp!.CheckResult(_passthrough.DestroyPassthroughLayerFB(_ptLayer), "DestroyPassthroughLayerFB");

                if (_ptInstance.Handle != 0)
                    _xrApp!.CheckResult(_passthrough.DestroyPassthroughFB(_ptInstance), "DestroyPassthroughFB");

                _ptInstance.Handle = 0;
                _ptLayer.Handle = 0;

            }

            _envDepth.Dispose();

            _meshes.Clear();

            base.Destroy();
        }

        public unsafe override void Create()
        {
   
            var caps = GetPtCapabilities();

            if ((caps.Capabilities & PassthroughCapabilityFlagsFB.BitFB) == 0)
                throw new NotSupportedException();

            _xrApp!.Xr.TryGetInstanceExtension<FBPassthrough>(null, _xrApp!.Instance, out _passthrough);

            CreatePt(PassthroughFlagsFB.IsRunningATCreationBitFB);

            CreatePtLayer(Purpose, PassthroughFlagsFB.IsRunningATCreationBitFB);

            _header->Type = StructureType.CompositionLayerPassthroughFB;

            if (UseEnvironmentDepth)
            {
                _envDepth.Create(_xrApp!);
                _envDepth.Start();
                _envDepth.RemoveHand(_removeHand);
            }

            _isStarted = true;

            base.Create();
        }

        protected override void OnEnabledChanged(bool isEnabled)
        {
            if (isEnabled == _isStarted)
                return;

            if (isEnabled)
            {
                StartPt();
                if (UseEnvironmentDepth)
                    _envDepth.Start();
            }

            else
            {
                PausePt();
                if (UseEnvironmentDepth)
                    _envDepth.Stop();
            }
        }

        protected override bool Update(ref CompositionLayerPassthroughFB layer, ref View[] views, long predTime)
        {
            layer.LayerHandle = _ptLayer;
            layer.Flags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
            return true;
        }

        public void UpdateMesh(XrPassthroughMesh mesh, Posef pose, Vector3f scale, Space baseSpace, long time)
        {
            var info = new GeometryInstanceTransformFB
            {
                Pose = pose,
                Scale = scale,
                BaseSpace = baseSpace,
                Time = time,
                Type = StructureType.GeometryInstanceTransformFB
            };

            _xrApp!.CheckResult(_passthrough!.GeometryInstanceSetTransformFB(mesh.Instance, in info), "GeometryInstanceSetTransformFB");
        }

        public XrPassthroughMesh AddMesh(Mesh mesh, Space baseSpace, object? tag = null)
        {
            var fbMesh = _xrApp!.Plugin<OculusXrPlugin>().CreateTriangleMesh(mesh.Indices!, mesh.Vertices!.Convert().To<Vector3f>());

            var info = new GeometryInstanceCreateInfoFB
            {
                Type = StructureType.GeometryInstanceCreateInfoFB,
                Mesh = fbMesh,
                Layer = _ptLayer,
                BaseSpace = baseSpace,
                Pose = new Posef
                {
                    Orientation = new Quaternionf(0, 0, 0, 1),
                },
                Scale = new Vector3f(1, 1, 1)
            };

            var instance = new GeometryInstanceFB();

            _xrApp.CheckResult(_passthrough!.CreateGeometryInstanceFB(_xrApp.Session, in info, ref instance), "CreateGeometryInstanceFB");

            var result = new XrPassthroughMesh
            {
                Instance = instance,
                Tag = tag,
                Mesh = fbMesh
            };

            _meshes.Add(result);

            return result;
        }

        public bool UseEnvironmentDepth { get; set; }

        public bool RemoveHand
        {
            get => _removeHand;
            set
            {
                if (value == _removeHand)
                    return;
                _removeHand = value;
                if (_envDepth.IsStarted)
                    _envDepth.RemoveHand(value);
            }
        }


        public XrEnvironmentDepth EnvironmentDepth => _envDepth;

        public EnvironmentDepthImageMETA? DepthImage => _depthImage;

        public override XrLayerFlags Flags => XrLayerFlags.EmptySpace;

        public PassthroughLayerPurposeFB Purpose { get; set; }

    }
}
