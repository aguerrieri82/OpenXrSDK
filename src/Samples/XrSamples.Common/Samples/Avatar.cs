using PhysX.Framework;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Oculus;
using static Sfizz.SfzParser;

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

                if (XrPlatform.IsAndroid)
                    await platform.LoginAsync("8587954307993093");
                else
                {
                    await platform.LoginAsync("test01_nvvjjf@tfbnw.net", "12345678", 8587954307993093);
                    //platform.Login("OCAQBiOGnu8iqz2cOQvbjSYlRduVbfv0z5fNUf515QjPZBhCDD1pRup81gdSvMOUZAgkOTQABhmZApV0jj8fSeDknVV9U2xmet5Ib8gawpQZDZD");
                    //await platform.LoginAsync("8587954307993093");
                }

                await avatarManager.LoginAsync(platform.AccessToken ?? throw new InvalidOperationException());

                var avatar = await avatarManager.LoadAsync("8672967276120323");
                var tracker = avatar.AddComponent<AvatarTracker>();

                tracker.Mirror(1f);

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
