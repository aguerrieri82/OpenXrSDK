using PhysX.Framework;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Oculus;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Avatar")]
        public static XrEngineAppBuilder CreateAvatar(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene;


            Task.Run(async () =>
            {
                var platform = Context.Require<OculusPlatform>();
                var avatar = Context.Require<OculusAvatarManager>();

                // await platform.LoginAsync("test01_nvvjjf@tfbnw.net", "12345678", 8587954307993093);

                await avatar.LoginAsync("OCAQBiOGnu8iqz2cOQvbjSYlRduVbfv0z5fNUf515QjPZBhCDD1pRup81gdSvMOUZAgkOTQABhmZApV0jj8fSeDknVV9U2xmet5Ib8gawpQZDZD");

                var obj = await avatar.LoadAsync("8672967276120323");

                var root = obj.FindByName<Joint3D>("root_joint")!;

                var builder = new StringBuilder();
                root.Print(builder);
                Log.Info(obj, builder.ToString());

                await EngineApp.MainThread;

                scene!.AddChild(obj);
            });

            return builder
                .UseApp(app)
                //.UseSceneModel(false, false)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .AddFloorShadow(4, false)
                .UsePhysics(new PhysicsOptions())
                .ConfigureSampleApp();
        }
    }
}
