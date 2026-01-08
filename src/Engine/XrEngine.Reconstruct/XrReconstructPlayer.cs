using System.ComponentModel;
using System.Numerics;
using XrEngine.Devices;
using XrMath;

namespace XrEngine.Reconstruct
{
    public class XrReconstructPlayer : BaseComponent<Scene3D>, IPlayer, INotifyPropertyChanged
    {
        readonly XrReconstructReader _reader;
        private int _frameNum;
        private PlayerState _playState;
        private bool _addShift;
        private float _projDistance;
        private float _cx;
        private float _cy;
        private float _fx;
        private float _fy;
        private readonly TriangleMesh _head;
        private readonly TriangleMesh _projection;
        private readonly TriangleMesh _cube;
        private readonly Texture2D _leftFrame;

        public XrReconstructPlayer()
        {
            _projDistance = 1.5f;
            _reader = XrReconstructReader.Current;
            _head = new TriangleMesh(new Cone3D(), (Material)MaterialFactory.CreatePbr(Color.Parse("#ff0000")));
            _head.Transform.SetScale(0.1f);
            _head.Transform.LocalPivot = Vector3.UnitZ;
            _head.Name = "Head";

            _cube = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr(Color.Parse("#ff0000")));
            _cube.Transform.SetScale(0.5f);
            _head.Transform.LocalPivot = new Vector3(0, -0.5f, 0);
            _cube.Name = "Cube";

            _leftFrame = new Texture2D();
            _leftFrame.BorderColor = Color.White;
            _leftFrame.WrapT = WrapMode.ClampToBorder;
            _leftFrame.WrapS = WrapMode.ClampToBorder;

            _projection = new TriangleMesh(Quad3D.Default, new TextureMaterial(_leftFrame)
            {
                Alpha = AlphaMode.Blend,
                Color = new Color(1, 1, 1, 0.7f),
                WriteDepth = false,
                UseDepth = false
            });
            _projection.Name = "Projection";
            _projection.Transform.Scale = new Vector3(1f, 1f, 0.01f);

            var sceneMat = (PbrV2Material)MaterialFactory.CreatePbr(Color.Parse("#ffffff"));
            sceneMat.ColorMap = _leftFrame;
            sceneMat.ColorMapProjection = Matrix4x4.Identity;

            _reader.SceneModel!.Materials.Add(sceneMat);
            _reader.SceneModel.Name = "Scene";
            _reader.SceneModel.SetWorldPose(_reader.Stats!.ScenePosition);

            AttachCamera = true;
        }

        Matrix4x4 ComputeProjViewMatrix(Matrix4x4 headMatrix, CameraParams cam)
        {
            float w = cam.SensorSize.Value.Width;
            float h = cam.SensorSize.Value.Height;
            var fx = cam.Intrinsic[0];
            var fy = cam.Intrinsic[1];
            var cx = (cam.Intrinsic.Length > 2) ? cam.Intrinsic[2] : w / 2.0f;
            var cy = (cam.Intrinsic.Length > 3) ? cam.Intrinsic[3] : h / 2.0f;

            var near = 0.1f;
            var far = 100.0f;


            var projection = new Matrix4x4();

            projection.M11 = (2.0f * fx) / w;
            projection.M22 = (2.0f * fy) / h;

            projection.M31 = 1.0f - (2.0f * cx) / w;
            projection.M32 = -(1.0f - (2.0f * cy) / h);

            projection.M33 = -(far + near) / (far - near);
            projection.M34 = -1.0f;

            projection.M43 = -(2.0f * far * near) / (far - near);
            projection.M44 = 0.0f;


            var sensorWorldMatrix = cam.GetLensPose().ToMatrix() * headMatrix;
            Matrix4x4 viewMatrix;
            Matrix4x4.Invert(sensorWorldMatrix, out viewMatrix);

            return viewMatrix * projection;
        }

        Matrix4x4 ComputeQuadMatrix(Matrix4x4 headMatrix, CameraParams cam, float distanceMeters)
        {
            if (cam.Intrinsic == null || cam.SensorSize == null)
                return Matrix4x4.Identity;

            var fx = cam.Intrinsic[0];
            var fy = cam.Intrinsic[1];
            var cx = cam.Intrinsic[2];
            var cy = cam.Intrinsic[3];
            var w = cam.SensorSize.Value.Width;
            var h = cam.SensorSize.Value.Height;

            var scaleX = distanceMeters * (w / fx);
            var scaleY = distanceMeters * (h / fy);

            var matScale = Matrix4x4.CreateScale(scaleX, scaleY, 1.0f);

            var shiftX = (w / 2.0f - cx) * (distanceMeters / fx);
            var shiftY = -(h / 2.0f - cy) * (distanceMeters / fy);

            if (!_addShift)
            {
                shiftX = 0;
                shiftY = 0;
            }

            var matTransLocal = Matrix4x4.CreateTranslation(shiftX, shiftY, -distanceMeters);

            var quadToSensor = matScale * matTransLocal;

            var sensorToHead = cam.GetLensPose().ToMatrix();

            return quadToSensor * sensorToHead * headMatrix;
        }

        protected override void OnAttach()
        {
            _host!.AddChild(_head);
            _host.AddChild(_projection);
            _host.AddChild(_cube);
            _host.AddChild(_reader.SceneModel!);

            base.OnAttach();
        }

        protected void LoadFrame()
        {
            LoadFrameScreen();
        }

        protected void LoadFrameColor()
        {
            var meta = _reader.Meta![_frameNum];

            var color = _reader.ReadColor(meta.Screen!.Frame).Left;

            _leftFrame.LoadData(new TextureData
            {
                Width = 1280,
                Height = 1280,
                Data = color.Data,
                Format = TextureFormat.Rgb24
            });

            var imageStat = _reader.Stats?.Images.FirstOrDefault(a => a.ImageTime / 1000 == meta.LeftColor.Time);

            var pose = (imageStat?.Pose ?? color.Pose!.Value);

            var camera = _reader.LeftCamera!;

            var sensorToHead = camera.GetLensPose();

            var combinedPose = pose.Multiply(sensorToHead);

            _head.SetWorldPose(combinedPose);

            _projection.WorldMatrix = ComputeQuadMatrix(pose.ToMatrix(), camera, _projDistance);

            if (AttachCamera)
                _host!.ActiveCamera!.SetWorldPose(combinedPose);

            ((PbrV2Material)_reader.SceneModel!.Materials[0]).ColorMapProjection = ComputeProjViewMatrix(pose.ToMatrix(), camera);


        }


        protected void LoadFrameScreen()
        {
            var metaFrame = _reader.Meta![_frameNum];

            var screenFrame = metaFrame.Screen!.Frame;

            var minDiff = long.MaxValue;
            RecordFrameData? metaColor = null;
            foreach (var meta in _reader.Meta)
            {
                var diff = Math.Abs(meta.LeftDepth!.Time / 1000 - metaFrame.Screen.Time);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    metaColor = meta;
                }
            }


            var screen = _reader.ReadScreen(screenFrame);

            _leftFrame.LoadData(new TextureData
            {
                Width = 1280,
                Height = 1280,
                Data = screen.Data,
                Format = TextureFormat.Rgb24
            });

            var proj = MathUtils.CreateMatrix(metaColor.LeftDepth!.Proj!);

            var eyePose = new Pose3()
            {
                Orientation = Quaternion.Identity,
                Position = new Vector3(-0.028116345f, 0.008583844f, 0.012929559f)
            };


            Matrix4x4.Invert(screen.View, out var word);
            Matrix4x4.Decompose(word, out var scale, out var rot, out var trans);

            var pose = new Pose3
            {
                Orientation = rot,
                Position = trans
            };

            //pose = metaFrame.Screen.Pose.Value;

            if (_frameNum == 0)
            {
                _fx = proj.M11 * 1280 / 2.0f;
                _fy = proj.M22 * 1280 / 2.0f;
                _cx = (1.0f - proj.M13) * 1280 / 2.0f;
                _cy = (1.0f - proj.M23) * 1280 / 2.0f;
                OnPropertyChanged(nameof(Fx));
                OnPropertyChanged(nameof(Fy));
                OnPropertyChanged(nameof(Cx));
                OnPropertyChanged(nameof(Cy));
            }

            var camera = new CameraParams
            {

                SensorSize = new Size2I
                {
                    Width = 1280,
                    Height = 1280
                },
                Intrinsic = [_fx, _fy, _cx, _cy]
            };

            _head.SetWorldPose(pose);

            _projection.WorldMatrix = ComputeQuadMatrix(pose.ToMatrix(), camera, _projDistance);

            if (AttachCamera)
                _host!.ActiveCamera!.SetWorldPose(pose);

            ((PbrV2Material)_reader.SceneModel!.Materials[0]).ColorMapProjection = ComputeProjViewMatrix(pose.ToMatrix(), camera);
            ((PbrV2Material)_reader.SceneModel!.Materials[0]).ColorMapProjection = Matrix4x4.Identity;
        }

        public void SetPlayState(PlayerState state)
        {
            _playState = state;

            if (state == PlayerState.Stop)
            {

                Frame = FirstFrame;
            }
        }


        public int Frame
        {
            get => _frameNum;
            set
            {
                var lastFrame = LastFrame > 0 ? LastFrame : Length - 1;

                value = Math.Min(Math.Max(0, value), lastFrame);

                if (value == _frameNum)
                    return;

                _frameNum = value;

                LoadFrame();

                OnPropertyChanged(nameof(Frame));
            }
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [Action]
        public void Export()
        {
            _reader.ExportFrames("d:\\out.json");
        }

        public PlayerState PlayState => _playState;

        public int Length => _reader.Meta?.Count ?? 0;

        public bool Loop { get; set; }

        public int FirstFrame { get; set; }

        public int LastFrame { get; set; }

        public bool AttachCamera { get; set; }



        public bool AddShift
        {
            get => _addShift;
            set
            {
                _addShift = value;
                LoadFrame();
            }
        }

        [Range(0.0f, 1280.0f, 1.0f)]
        public float Cx
        {
            get => _cx;
            set
            {
                _cx = value;
                OnPropertyChanged(nameof(Cx));
                LoadFrame();
            }
        }

        [Range(0.0f, 1280.0f, 1.0f)]
        public float Cy
        {
            get => _cy;
            set
            {
                _cy = value;
                OnPropertyChanged(nameof(Cy));
                LoadFrame();
            }
        }


        [Range(0.0f, 1280.0f, 1.0f)]
        public float Fx
        {
            get => _fx;
            set
            {
                _fx = value;
                OnPropertyChanged(nameof(Fx));
                LoadFrame();
            }
        }

        [Range(0.0f, 1280.0f, 1.0f)]
        public float Fy
        {
            get => _fy;
            set
            {
                _fy = value;
                OnPropertyChanged(nameof(Fy));
                LoadFrame();
            }
        }

        [Range(0.5f, 4f, 0.1f)]
        public float ProjDistance
        {
            get => _projDistance;
            set
            {
                _projDistance = value;
                LoadFrame();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
