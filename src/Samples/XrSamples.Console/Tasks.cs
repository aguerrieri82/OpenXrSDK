using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Assimp;
using System.Globalization;
using System.Numerics;
using System.Text;
using VirtualCamera.IPCamera;
using XrEngine;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.OpenXr.Windows;
using XrEngine.Video;
using File = System.IO.File;


namespace XrSamples
{
    public class Tasks
    {
        public static IServiceProvider? Services { get; set; }

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
