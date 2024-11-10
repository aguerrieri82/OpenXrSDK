using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PlanarReflection : Behavior<Object3D>, IDrawGizmos
    {

        private float _lightIntensity;
        private Plane _plane;

        public PlanarReflection()
            : this(1024)
        {

        }

        public PlanarReflection(uint textureSize)
        {
            AdjustIbl = true;   

            TextureSize = textureSize;

            ReflectionCamera = new PerspectiveCamera();

            MaterialOverride = new TextureMaterial()
            {
                CheckTexture = true
            };

            Texture = new Texture2D
            {
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                Depth = IsMultiView ? 2u : 1u,
                Flags = EngineObjectFlags.Mutable,
                MipLevelCount = 1
            };

            Texture.LoadData(new TextureData
            {
                Width = TextureSize,
                Height = TextureSize,
                Depth = IsMultiView ? 2u : 1u,
                Format = TextureFormat.SRgba32
            });

            Offset = 0.01f;
            FovDegree = 45;
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

        protected void AdjustFov()
        {
            ReflectionCamera.SetFov(FovDegree, ReflectionCamera.ViewSize.Width, ReflectionCamera.ViewSize.Height);

            _host!.UpdateBounds(true);

            var bounds = _host!.WorldBounds!.Points.Select(a => ReflectionCamera.Project(a)).ComputeBounds();
            var scaleRatio = 2.0f / MathF.Max(bounds.Size.X, bounds.Size.Y);
            var newFovRadians = 2.0f * MathF.Atan(MathF.Tan(FovDegree / 2.0f) / scaleRatio);
            if (!float.IsNaN(newFovRadians))
            {
                ReflectionCamera.FovDegree = newFovRadians * 180.0f / MathF.PI;
                ReflectionCamera.UpdateProjection();
            }
        }

        protected override void Update(RenderContext ctx)
        {
            var camera = ctx.Scene?.ActiveCamera;

            if (camera != null)
                Update(camera);
        }

        public void Update(Camera camera)
        {
            var normal = _host!.Forward.Normalize();

            _host.UpdateBounds(true);

            var pos = _host!.WorldBounds.Center;

            _plane = new Plane(normal, -Vector3.Dot(normal, pos) + Offset);

            ReflectionCamera.Far = camera.Far;
            ReflectionCamera.Near = camera.Near;

            var ratio = camera.ViewSize.Width / (float)camera.ViewSize.Height;
            var curSize = new Size2
            {
                Width = TextureSize,
                Height = (int)(TextureSize / ratio)
            };

            if (curSize.Width != Texture.Width || curSize.Height != Texture.Height)
            {
                Texture.LoadData(new TextureData
                {
                    Width = (uint)curSize.Width,
                    Height = (uint)curSize.Height,
                    Depth = Texture.Depth,
                    Format = Texture.Format
                });

                ReflectionCamera.ViewSize = new Size2I
                {
                    Width = Texture.Width,
                    Height = Texture.Height
                };
            }

            if (IsMultiView)
            {
                Debug.Assert(camera.Eyes != null && camera.Eyes.Length == 2);

                for (var i = 0; i < 2; i++)
                {
                    var world = camera.Eyes[i].World;

                    var cameraPos = world.Translation;

                    var distance = Vector3.Dot(normal, cameraPos - pos);

                    var forward = -Vector3.UnitZ.ToDirection(world);
                    var up = Vector3.UnitY.ToDirection(world);

                    var refPos = cameraPos - 2 * distance * normal;

                    var refView = Matrix4x4.CreateLookAt(
                        refPos,
                        refPos + Vector3.Reflect(forward, normal),
                        Vector3.Reflect(up, normal)
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

                var distance = Vector3.Dot(normal, cameraPos - pos);

                var refPos = cameraPos - 2 * distance * normal;

                var refView = Matrix4x4.CreateLookAt(
                    refPos,
                    refPos + Vector3.Reflect(camera.Forward, normal),
                    Vector3.Reflect(camera.Up, normal)
                );

                ReflectionCamera.View = refView;
                ReflectionCamera.SetFov(FovDegree, Texture.Width, Texture.Height);

                //ReflectionCamera.Projection = camera.Projection;
                //ReflectionCamera.View = camera.View * Matrix4x4.CreateReflection(plane);
            }

            if (AutoAdjustFov)
                AdjustFov();

            if (_host?.Scene != null)
                _lightIntensity = _host.Scene.Descendants<Light>().Visible().Select(a => a.Intensity).Sum();
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            /*
            var bounds = _host!.WorldBounds;
            var pos = bounds.Center;
            canvas.Save();
            canvas.State.Color = "#00ff00";
            canvas.DrawPlane(_plane, pos, 1, 2, 0.1f);
            canvas.DrawLine(pos, pos + _plane.Normal);
            canvas.Restore();
            */
        }

        [Range(-1, 1, 0.001f)]
        public float Offset { get; set; }

        public bool RenderEnvironment { get; set; }

        public PerspectiveCamera ReflectionCamera { get; }

        public Texture2D Texture { get; set; }

        public TextureMaterial MaterialOverride { get; set; }

        [Range(1, 180, 1)]
        public float FovDegree { get; set; }

        public uint TextureSize { get; set; }

        public bool AutoAdjustFov { get; set; }

        public bool UseClipPlane { get; set; }  

        public bool AdjustIbl { get; set; }

        public Plane Plane => _plane;

        public static bool IsMultiView { get; set; }

    }
}
