using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Cube")]
        public static XrEngineAppBuilder CreateCube(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Sphere3D.Default, (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "mesh"
            };

            cube.Transform.SetScale(0.1f);
            cube.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI / 4f);
            cube.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(cube);

            /*
            var quad = new TriangleMesh(new Quad3D(new Vector2(2, 2)), new TextureClipMaterial
            {
                Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/check.png"),
                Alpha = AlphaMode.Opaque,
                Color = new Color(1, 1, 1, 0.7f),
                WriteDepth = false,
                UseDepth = false,
                DoubleSided = true
            });

            app.ActiveScene!.AddChild(quad);

            app.ActiveScene.AddBehavior((_, ctx) =>
            {
                if (XrApp.Current != null && XrApp.Current.IsStarted)
                {
                    var mesh = XrApp.Current.GetVisibilityMask(0, Silk.NET.OpenXR.VisibilityMaskTypeKHR.LineLoopKhr);
                    if (ctx.Scene?.ActiveCamera?.Eyes != null)
                    {
                        var v3 = mesh.Vertices.Select(a => new Vector3(a.X, a.Y, -1)).ToArray();
                        var proj = ctx.Scene.ActiveCamera.Eyes[0].Projection;
                        var projVert = v3.Select(a => a.Project(proj)).ToArray();

                        var builder = new Bounds3Builder();
                        builder.Add(projVert);
                        var bb = builder.Result;
                    }

                }

            });
            */

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }
    }
}
