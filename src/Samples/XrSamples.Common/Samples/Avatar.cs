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
                var avatarManager = Context.Require<OculusAvatarManager>();
                // await platform.LoginAsync("test01_nvvjjf@tfbnw.net", "12345678", 8587954307993093);

                await avatarManager.LoginAsync("OCAQBiOGnu8iqz2cOQvbjSYlRduVbfv0z5fNUf515QjPZBhCDD1pRup81gdSvMOUZAgkOTQABhmZApV0jj8fSeDknVV9U2xmet5Ib8gawpQZDZD");

                var avatar = await avatarManager.LoadAsync("8672967276120323");
                avatar.Mirror(1f);

                await EngineApp.MainThread;

                scene!.AddChild(avatar);
            });

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseOculus(opt =>
                {
                    opt.UseBodyTrack = true;
                    opt.UseBothHandAndControllers = false;
                })
                .ConfigureSampleApp(useHands: false);
        }
    }
}
