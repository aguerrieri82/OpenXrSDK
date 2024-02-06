using OpenXr.Framework.Vulkan;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oculus.XrPlugin;
using static OVRPlugin;
using static Oculus.XrPlugin.OculusXrPlugin;

namespace OpenXr
{
    public static class Tasks
    {
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

        public static async Task AnchorsTask()
        {
            var vulkan = new VulkanDevice();
            var xrOculus = new OpenXr.Framework.Oculus.OculusXrPlugin();
            var app = new XrApp(new VulkanGraphicDriver(vulkan), xrOculus);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    app.HandleEvents();
                    await Task.Delay(50);
                }
            });

            while (true)
            {

                app.Start();

                var res = await xrOculus.QueryAllAnchorsAsync().ConfigureAwait(true);

                var roomSpace = xrOculus.SpaceWithComponents(res, SpaceComponentTypeFB.RoomLayoutFB).First();

                var roomLayout = xrOculus.GetSpaceRoomLayout(roomSpace.Space);

                var walls = roomLayout.GetWalls();

                foreach (var space in res)
                {
                    var components = xrOculus.GetSpaceSupportedComponents(space.Space);

                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.SemanticLabelsFB))
                    {
                        var label = xrOculus.GetSpaceSemanticLabels(space.Space);


                        Console.WriteLine(label[0]);
                    }


                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.Bounded2DFB))
                    {
                        try
                        {
                            var bounds = xrOculus.GetSpaceBoundingBox2D(space.Space);
                            Console.WriteLine(bounds);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.LocatableFB))
                    {
                        var local = app.LocateSpace(app.Stage, space.Space, 1);

                        Console.WriteLine(local.Pose);
                    }

                    if (xrOculus.GetSpaceComponentEnabled(space.Space, OpenXr.Framework.Oculus.OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META))
                    {
                        try
                        {
                            var mesh = xrOculus.GetSpaceTriangleMesh(space.Space);
                            Console.WriteLine(mesh);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }


                }

                app.Stop();

                if (Console.ReadKey().Key == ConsoleKey.Enter)
                    break;
            }

            app.Dispose();

        }

        public static async Task WebLinkTask()
        {
            var client = new WebLink.Client.WebLinkClient("https://localhost:7135");
            
            await client.ConnectAsync("");

            await client.StartSessionAsync();

            var anchors = await client.GetAnchorsAsync(new WebLink.Entities.XrAnchorFilter
            {
                Components = WebLink.Entities.XrAnchorComponent.All
            });

            await client.StopSessionAsync();

        }
    }
}
