
using OpenXr.Engine;
using Silk.NET.Assimp;
using System.Globalization;
using System.Numerics;
using System.Text;
using static Oculus.XrPlugin.OculusXrPlugin;
using static OVRPlugin;
using File = System.IO.File;


namespace OpenXr
{
    public static class Tasks
    {
        public static IServiceProvider? Services { get; set; }

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
                List<VertexData> vertices = new List<VertexData>();
                List<uint> indices = new List<uint>();

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

        public unsafe static void LoadTexture()
        {
            var reader = new PvrReader();
            using var stream = File.OpenRead(@"d:\TestScreen.pvr");
            reader.Read(stream);
        }

        public static Task OvrLibTask()
        {

            static bool CheckResult(OVRPlugin.Result result)
            {
                if (result != OVRPlugin.Result.Success)
                {
                    Console.WriteLine("ERR: " + result);
                    return false;
                }
                return true;
            }

            Bool boolRes;

            var res2 = LoadOVRPlugin(null);

            var isSup = GetIsSupportedDevice();


            var settings = new UserDefinedSettings()
            {
            };

            SetUserDefinedSettings(settings);

            var headSet = GetSystemHeadsetType();


            var isInit = OVRP_1_1_0.ovrp_GetInitialized();


            boolRes = OVRP_OBSOLETE.ovrp_PreInitialize();
            boolRes = OVRP_OBSOLETE.ovrp_Initialize(5, 0);

            CheckResult(OVRP_1_15_0.ovrp_InitializeMixedReality());


            CheckResult(OVRP_1_15_0.ovrp_InitializeMixedReality());
            CheckResult(OVRP_1_63_0.ovrp_InitializeInsightPassthrough());


            CheckResult(OVRP_1_55_0.ovrp_GetNativeOpenXRHandles(out var inst, out var sess));
            CheckResult(OVRP_1_55_0.ovrp_GetNativeXrApiType(out var xrApi));


            CheckResult(OVRP_1_38_0.ovrp_Media_Initialize());
            CheckResult(OVRP_1_15_0.ovrp_GetExternalCameraCount(out var cameraCount));


            var desc = new LayerDesc();

            var size = new Sizei
            {
                h = 100,
                w = 100
            };

            int layerId;

            unsafe
            {
                CheckResult(OVRP_1_15_0.ovrp_CalculateLayerDesc(OverlayShape.Quad, LayerLayout.Mono, ref size, 1, 1, EyeTextureFormat.R8G8B8A8_sRGB, 0, ref desc));

                CheckResult(OVRP_1_28_0.ovrp_EnqueueSetupLayer2(ref desc, 1, &layerId));

            }

            var x = OVRPlugin.OVRP_1_1_0.ovrp_GetVersion();

            Console.WriteLine(x);


            unsafe
            {

                var info = new SpaceQueryInfo();
                info.MaxQuerySpaces = 10;
                info.Location = SpaceStorageLocation.Local;
                info.ComponentsInfo.Components = new SpaceComponentType[16];
                info.ComponentsInfo.Components[0] = SpaceComponentType.RoomLayout;
                info.ComponentsInfo.Components[1] = SpaceComponentType.SemanticLabels;
                info.ComponentsInfo.Components[2] = SpaceComponentType.TriangleMesh;
                info.ComponentsInfo.Components[3] = SpaceComponentType.Bounded2D;
                info.ComponentsInfo.Components[4] = SpaceComponentType.Bounded3D;
                info.ComponentsInfo.NumComponents = 5;

                CheckResult(OVRP_1_72_0.ovrp_QuerySpaces(ref info, out var reqId));

                uint count = 0;
                CheckResult(OVRP_1_72_0.ovrp_RetrieveSpaceQueryResults(ref reqId, default, ref count, default));

                var results = new SpaceQueryResult[count];

                fixed (SpaceQueryResult* resultsPtr = results)
                    CheckResult(OVRP_1_72_0.ovrp_RetrieveSpaceQueryResults(ref reqId, count, ref count, new nint(&resultsPtr)));

                Console.WriteLine("Hello");

            }

            return Task.CompletedTask;
        }

    }
}
