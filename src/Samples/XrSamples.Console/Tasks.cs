using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sfizz;
using Silk.NET.Assimp;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using VirtualCamera.IPCamera;
using XrEngine;
using XrEngine.AI;
using XrEngine.Compression;
using XrEngine.Devices;
using XrEngine.Devices.Windows;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Windows;
using XrEngine.Video;
using XrMath;
using File = System.IO.File;


namespace XrSamples
{
    public class Tasks
    {
        public static IServiceProvider? Services { get; set; }

        public static void ParseSfz()
        {
            var file = @"D:\SoundFont\WilkinsonAudio.NakedDrums\Wilkinson Audio\Naked Drums\User\Naked Drums GM.sfz";
            var parser = new SfzParser();
            parser.Parse(file);
            var size = parser.SamplesSize();
            //parser.CopyTo("d:\\test\\");

        }

        public static async Task TestBlePedalAsync()
        {
            Context.Implement<ILogger>(Services!.GetService<ILogger<Tasks>>()!);
            Context.Implement<IProgressLogger>(new ProgressLogger());

            var manager = new WinBleManager();


            Log.Info(typeof(Tasks), "Find device...");

            var devices = await manager.FindDevicesAsync(new BleDeviceFilter
            {
                Name = "Pedal Controller",
                MaxDevices = 1,
                Timeout = TimeSpan.FromSeconds(10)
            });

            if (devices.Count == 0)
            {
                Log.Warn(typeof(Tasks), "No devices found");
                return;
            }


            var pedal = new BlePedal(manager);

            Log.Info(typeof(Tasks), "Connecting");

            //await pedal.ConnectAsync(devices[0].Address);

            await pedal.ConnectAsync(225243778289514);

            Log.Info(typeof(Tasks), "Read Settings");

            var set = await pedal.ReadSettingsAsync();

            set.RampUp = 900;
            set.RampHit = 1390;
            set.RampDown = 1200;
            set.SampleRate = 100;
            set.Mode = (byte)'H';


            Log.Info(typeof(Tasks), "Update Settings");

            await pedal.UpdateSettingsAsync(set);

            Log.Info(typeof(Tasks), "Wait for data...");

            pedal.Data += (sender, args) =>
            {
                Log.Debug("", "Hit: {0}", args.Data.Value);
            };

            while (pedal.IsConnected)
            {
                var bat = await pedal.GetBatteryAsync();

                Log.Info("", "Battery: {0}", MathF.Round(bat, 2));

                await Task.Delay(30000);
            }

            Console.ReadKey();
        }


        public static void TrainPosePredictor()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };

            var json = File.ReadAllText(Path.Join("D:\\Projects\\XrEditor", "inputs.json"));

            var session = JsonSerializer.Deserialize<XrInputRecorder.RecordSession>(json, options);


            var data = new List<PoseTrainData>();

            foreach (var frame in session.Frames)
            {
                if (!frame.Inputs.TryGetValue("RightGripPose", out var pose))
                    continue;
                var value = pose.Value;
                if (value is JsonElement je)
                    value = je.Deserialize<Pose3>(new JsonSerializerOptions { IncludeFields = true })!;

                data.Add(new PoseTrainData
                {
                    Pose = (Pose3)value,
                    Time = (float)frame.Time
                });
            }

            var pred = new AIPosePredictorCore(7, "d:\\pose_prediction_model");
            //pred.Train(data);
            for (var i = 8; i < data.Count; i++)
            {
                var slice = data.Skip(i - 8).Take(8).ToArray();
                var test = pred.Predict(slice);
                var control = data[i];
                Console.WriteLine(test.Pose.Position - control.Pose.Position);
            }



        }

        public static void ReadRtsp()
        {
            Context.Implement<ILogger>(Services!.GetService<ILogger<Tasks>>()!);
            Context.Implement<IProgressLogger>(new ProgressLogger());

            Log.Debug("", "Rtsp: Connect");

            var uri = new Uri("rtsp://192.168.1.89:554/videodevice");

            var client = new RtspClient();
            client.Connect(uri.Host, uri.Port);

            var streamName = uri.ToString();

            Log.Debug("", "Rtsp: Describe");

            var streams = client.Describe(streamName);

            var videoStream = streams.FirstOrDefault(a => a.Type == RtspStreamType.Video);
            if (videoStream == null)
                throw new InvalidOperationException();


            Log.Debug("", "Rtsp: Setup");

            var session = client.Setup(videoStream, 1100);

            if (session == null)
                throw new InvalidOperationException();


            var h264Stream = new RtpH264Client(session.ClientPort);
            h264Stream.Open();

            if (!client.Play(streamName, session!))
                throw new InvalidOperationException();

            while (true)
            {
                var res = h264Stream.ReadNalUnit();
            }

        }

        public static unsafe void WriteMesh(string fileName)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            void ProcessNode(Silk.NET.Assimp.Node* node, Silk.NET.Assimp.Scene* scene)
            {
                for (var i = 0; i < node->MNumMeshes; i++)
                {
                    var mesh = scene->MMeshes[node->MMeshes[i]];
                    ProcessMesh(mesh, scene);

                }

                for (var i = 0; i < node->MNumChildren; i++)
                {
                    ProcessNode(node->MChildren[i], scene);
                }
            }

            unsafe void ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Silk.NET.Assimp.Scene* scene)
            {
                List<VertexData> vertices = new();
                List<uint> indices = new();

                for (uint i = 0; i < mesh->MNumVertices; i++)
                {
                    var vertex = new VertexData();
                    vertex.Pos = mesh->MVertices[i];
                    if (mesh->MNormals != null)
                        vertex.Normal = mesh->MNormals[i];

                    if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
                    {
                        var texcoord3 = mesh->MTextureCoords[0][i];
                        vertex.UV = new Vector2(texcoord3.X, texcoord3.Y);
                    }

                    vertices.Add(vertex);
                }

                for (uint i = 0; i < mesh->MNumFaces; i++)
                {
                    var face = mesh->MFaces[i];

                    for (uint j = 0; j < face.MNumIndices; j++)
                        indices.Add(face.MIndices[j]);
                }

                var buffer = new StringBuilder();

                for (var i = 0; i < vertices.Count; i++)
                {
                    var v = vertices[i].Pos;
                    var n = vertices[i].Normal;
                    var u = vertices[i].UV;

                    buffer.Append($"{v.X}f, {v.Y}f, {v.Z}f, {n.X}f, {n.Y}f, {n.Z}f, {u.X}f, {u.Y}f,\n");
                }

                System.Diagnostics.Debug.WriteLine(buffer.ToString());

                buffer.Clear();

                for (var i = 0; i < indices.Count; i++)
                {
                    if (i > 0 && i % 3 == 0)
                        buffer.Append("\n");
                    buffer.Append($"{indices[i]},");

                }

                System.Diagnostics.Debug.WriteLine(buffer.ToString());
            }

            var assimp = Assimp.GetApi();

            var scene = assimp.ImportFile(fileName, (uint)PostProcessSteps.Triangulate);

            ProcessNode(scene->MRootNode, scene);
        }

        public static void TestPivot()
        {
            var obj = new Object3D();

            var pivot = new Vector3(10, -3, 2);
            var pos1 = new Vector3(2, 1, -2);
            var pos2 = new Vector3(2, -2, 6);
            var ori = Quaternion.CreateFromYawPitchRoll(0.3f, -2, 3.3f);

            obj.Transform.SetLocalPivot(pivot, true);

            Debug.Assert(obj.WorldPosition.IsSimilar(pivot));

            obj.WorldPosition = pos1;

            var zero = obj.ToWorld(Vector3.Zero);
            var wordPivot = obj.ToWorld(pivot);

            Debug.Assert(wordPivot.IsSimilar(pos1));

            obj.WorldOrientation = ori;

            wordPivot = obj.ToWorld(pivot);

            Debug.Assert(wordPivot.IsSimilar(pos1));

            obj.MoveLocalToWorld(Vector3.Zero, Vector3.Zero);

            zero = obj.ToWorld(Vector3.Zero);

            Debug.Assert(zero.IsSimilar(Vector3.Zero));

            var parent = new Group3D();
            parent.Transform.SetScale(1.4f, 1, 2);
            parent.Transform.Position = new Vector3(10, -2, 4);
            parent.Transform.Orientation = Quaternion.CreateFromYawPitchRoll(1.3f, 1.2f, -4f);
            parent.Transform.LocalPivot = new Vector3(-223, 11, 3);

            parent.AddChild(obj, false);

            zero = obj.ToWorld(Vector3.Zero);

            Debug.Assert(!zero.IsSimilar(Vector3.Zero));

            obj.MoveLocalToWorld(Vector3.Zero, Vector3.Zero);

            zero = obj.ToWorld(Vector3.Zero);

            Debug.Assert(zero.IsSimilar(Vector3.Zero));

            var newPose = new Pose3
            {
                Position = pos2,
                Orientation = ori,
            };

            obj.SetWorldPose(newPose, true);

            wordPivot = obj.ToWorld(pivot);
            zero = obj.ToWorld(Vector3.Zero);

            Debug.Assert(zero.IsSimilar(pos2));

            Debug.Assert(obj.WorldOrientation.IsSimilar(ori));

            var curPose = obj.GetWorldPose(true);

            Debug.Assert(curPose.IsSimilar(newPose, 1e-5f));

            obj.Transform.SetLocalPivot(Vector3.Zero, true);

            curPose = obj.GetWorldPose(true);

            Debug.Assert(curPose.IsSimilar(newPose, 1e-5f));

            var curPose2 = obj.GetWorldPose(false);

        }


        public static void LoadModel(string path)
        {
            GltfLoader.LoadFile(path);
        }

        public static void CompressTexture(string path)
        {
            var data = EtcCompressor.Encode(path, 16);
            PvrTranscoder.Instance.SaveTexture(File.OpenWrite("d:\\test.pvr"), data);
        }

        public unsafe static void LoadTexture()
        {
            var reader = PvrTranscoder.Instance; ;
            using var stream = File.OpenRead(@"d:\TestScreen.pvr");
            reader.LoadTexture(stream);
        }


    }
}
