using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenXr;

namespace Game
{
    public static class Builder 
    {
        public static XrEngineAppBuilder CreateGame(this XrEngineAppBuilder builder)
        {
            var app = new EngineApp();

            var scene = new Scene3D();

            scene.AddComponent<AudioSystem>();

            scene.ActiveCamera = scene.AddChild(new PerspectiveCamera());

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            scene.AddChild(new TriangleMesh(Cube3D.Default, new PbrMaterial() { Color = "#ff0000" }));

            var light = scene.AddChild<ImageLight>();
            light.Intensity = 1f;

            light.LoadPanorama("res://asset/Envs/Pisa.hdr");

            app.OpenScene(scene);

            return builder
                .UseApp(app)
                .ConfigureApp(_ => { })
                .UseLeftController()
                .UseRightController()
                .UseInputs<XrOculusTouchController>(a => a
                       .AddAction(b => b.Right!.Thumbstick)
                       .AddAction(b => b.Right!.Haptic)
                       .AddAction(b => b.Left!.Haptic))
                .AddRightPointer()
                .UseRayCollider()
                .AddXrRoot()
                .UseOculus(opt =>
                {
                    opt.UseBothHandAndControllers = false;
                });
        }
    }
}
