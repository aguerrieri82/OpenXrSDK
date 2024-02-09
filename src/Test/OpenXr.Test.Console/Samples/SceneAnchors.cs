using Microsoft.Extensions.Logging;
using OpenXr.Framework.OpenGL;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OpenXr.Samples
{
    public class SceneAnchors
    {
        public static async Task Run(IServiceProvider services, ILogger logger)
        {
   
            var xrOculus = new Framework.Oculus.OculusXrPlugin();

            var app = new XrApp(services!.GetRequiredService<ILogger<XrApp>>(),
                    //new XrVulkanGraphicDriver(new VulkanDevice()),
                    new XrOpenGLGraphicDriver(new OpenGLDevice()),
                xrOculus);

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

                app.Start(XrAppStartMode.Query);

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


                        logger.LogInformation(label[0]);
                    }


                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.Bounded2DFB))
                    {
                        try
                        {
                            var bounds = xrOculus.GetSpaceBoundingBox2D(space.Space);
                            logger.LogInformation(bounds.ToString());
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.ToString());
                        }
                    }

                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.LocatableFB))
                    {
                        var local = app.LocateSpace(app.Stage, space.Space, 1);

                        logger.LogInformation(local.Pose.ToString());
                    }

                    if (xrOculus.GetSpaceComponentEnabled(space.Space, OpenXr.Framework.Oculus.OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META))
                    {
                        try
                        {
                            var mesh = xrOculus.GetSpaceTriangleMesh(space.Space);
                            logger.LogInformation(mesh.ToString());
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.ToString());
                        }
                    }
                }

                app.Stop();

                if (Console.ReadKey().Key == ConsoleKey.Enter)
                    break;
            }

            app.Dispose();
        }
    }
}
