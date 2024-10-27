using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PlanarReflection : Behavior<Object3D>, IDrawGizmos
    {
        private Plane _plane;
        private float _lightIntensity;

        public PlanarReflection()
            : this(1024)
        {

        }

        public PlanarReflection(uint textureSize)
        {
            ReflectionCamera = new PerspectiveCamera();

            MaterialOverride = new TextureMaterial()
            {
                CheckTexture = true
            };

            Texture = new Texture2D
            {
                Width = textureSize,
                Height = textureSize,
                Format = TextureFormat.Rgba32,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                Depth = IsMultiView ? 2u : 1u,
                MipLevelCount = 1
            };
        }


        public virtual bool PrepareMaterial(Material material)
        {
            if (material is IPbrMaterial pbr)
            {
                if (pbr.Alpha == AlphaMode.BlendMain || pbr.Alpha == AlphaMode.Blend)
                    return false;

                MaterialOverride.Texture = pbr.ColorMap;
                MaterialOverride.Color = pbr.Color * MathF.Min(1, _lightIntensity * 0.5f);
                MaterialOverride.Alpha = pbr.Alpha; 
                return true;
            }

            return false;
        }


        protected override void Update(RenderContext ctx)
        {
            var camera = ctx.Scene?.ActiveCamera;

            if (camera == null)
                return;

            var normal = -_host!.Forward;
            _plane = new Plane(normal, -Vector3.Dot(normal, _host.WorldPosition));

            ReflectionCamera.Far = camera.Far;
            ReflectionCamera.Near = camera.Near;

            float d = Vector3.Dot(normal, _host.WorldPosition);

            if (IsMultiView)
            {
                Debug.Assert(camera.Eyes != null && camera.Eyes.Length == 2);

                for (var i = 0; i < 2; i++)
                {
                    var world = camera.Eyes[i].World;

                    var cameraPos = camera.WorldPosition;
                    var distance = Vector3.Dot(normal, cameraPos) - d;

                    var forward = -Vector3.UnitZ.ToDirection(world);
                    var up = Vector3.UnitY.ToDirection(world);

                    var refPos = cameraPos - 2 * distance * normal;

                    var refView = Matrix4x4.CreateLookAt(
                        refPos,
                        refPos + Vector3.Reflect(forward, normal),
                        up
                    );

                    if (ReflectionCamera.Eyes == null)
                        ReflectionCamera.Eyes = new CameraEye[2];

                    ReflectionCamera.Eyes[i].World = world;
                    ReflectionCamera.Eyes[i].Projection = camera.Projection;
                    ReflectionCamera.Eyes[i].View = refView;
                    ReflectionCamera.Eyes[i].ViewProj = refView * camera.Projection;
                }
            }
            else
            {
                var cameraPos = camera.WorldPosition;

                var distance = Vector3.Dot(normal, cameraPos) - d;

                var refPos = cameraPos - 2 * distance * normal;

                var refView = Matrix4x4.CreateLookAt(
                    refPos,
                    refPos + Vector3.Reflect(camera.Forward, normal),
                    camera.Up
                );

                ReflectionCamera.Projection = camera.Projection;
                ReflectionCamera.View = refView;
            }

            _lightIntensity = _host!.Scene!.Descendants<Light>().Visible().Select(a => a.Intensity).Sum();
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            /*
            var bounds = _host!.WorldBounds;
            canvas.Save();
            canvas.State.Color = "#00ff00";
            canvas.DrawPlane(_plane);
            canvas.DrawLine(_host.WorldBounds.Center, _host.WorldBounds.Center + _plane.Normal);
            canvas.Restore();
            */
        }

        public bool RenderEnvironment { get; set; }

        public Camera ReflectionCamera { get; }

        public Texture2D Texture { get; set; }

        public TextureMaterial MaterialOverride { get; set; }

        public static bool IsMultiView { get; set; }
    }
}
