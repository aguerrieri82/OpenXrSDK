using System.Diagnostics;
using System.Numerics;
using System.Xml.Linq;
using XrMath;

namespace XrEngine
{
    public enum PlanarReflectionMode
    {
        ColorOnly,
        Basic,
        Full
    }

    public class PlanarReflection : BaseComponent<Object3D>, IDrawGizmos
    {
        private float _lightIntensity;
        private Plane _plane;
        private PlanarReflectionMode _mode;

        public PlanarReflection()
            : this(1024)
        {

        }

        public PlanarReflection(uint textureSize, PlanarReflectionMode mode = PlanarReflectionMode.ColorOnly)
        {
            _mode = mode;

            AdjustIbl = true;   

            TextureSize = textureSize;

            ReflectionCamera = new PerspectiveCamera();

            if (_mode== PlanarReflectionMode.ColorOnly)
            {
                MaterialOverride = new TextureMaterial()
                {
                    CheckTexture = true
                };
            }
            else if (mode == PlanarReflectionMode.Basic)
            {
                MaterialOverride = new BasicMaterial();
            }
         
            Offset = 0.01f;
            FovDegree = 45;
        }

        public virtual bool PrepareMaterial(Material material)
        {
            if (MaterialOverride == null)
                return false;   

            MaterialOverride.UseClipDistance = UseClipPlane;        

            if (material is IPbrMaterial pbr)
            {
                if (pbr.Alpha == AlphaMode.BlendMain || pbr.Alpha == AlphaMode.Blend)
                    return false;

                if (MaterialOverride is TextureMaterial tex)
                {
                    tex.Texture = pbr.ColorMap;
                    tex.Color = pbr.Color * MathF.Min(1, _lightIntensity * 0.5f);
                }
                else if (MaterialOverride is BasicMaterial bsc)
                {
                    var texChanged = (bsc.DiffuseTexture == null && pbr.ColorMap != null ||
                                      bsc.DiffuseTexture != null && pbr.ColorMap == null);

                    bsc.Shininess = 10;
    
                    bsc.DiffuseTexture = null;
                    bsc.DiffuseTexture = pbr.ColorMap;
                    bsc.Ambient = Color.White;
                    bsc.Specular = Color.White;
                    bsc.Color = pbr.Color * MathF.Min(1, _lightIntensity * 0.5f);

                    if (texChanged)
                        bsc.NotifyChanged(ObjectChangeType.Render);

                }

                MaterialOverride.DoubleSided = pbr.DoubleSided;
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

        public void Update(Camera camera, int boundEye)
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

            if (Texture == null || curSize.Width != Texture.Width || curSize.Height != Texture.Height)
            {
                Texture?.Dispose();

                Texture = new Texture2D
                {
                    MagFilter = ScaleFilter.Linear,
                    MinFilter = ScaleFilter.Linear,
                    WrapS = WrapMode.ClampToEdge,
                    WrapT = WrapMode.ClampToEdge,
                    Depth = IsMultiView ? 2u : 1u,
                    MipLevelCount = 1
                };

                Texture.LoadData(new TextureData
                {
                    Width = (uint)curSize.Width,
                    Height = (uint)curSize.Height,
                    Depth = IsMultiView ? 2u : 1u,
                    Format = UseSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32
                });

                ReflectionCamera.ViewSize = new Size2I
                {
                    Width = Texture.Width,
                    Height = Texture.Height
                };
            }

            ReflectionCamera.SetFov(FovDegree, Texture.Width, Texture.Height);

            if (IsMultiView && boundEye == -1)
            {
                Debug.Assert(camera.Eyes != null && camera.Eyes.Length == 2);

                for (var i = 0; i < 2; i++)
                {
                    var curWorld = camera.Eyes[i].World;

                    var cameraPos = curWorld.Translation;

                    var distance = Vector3.Dot(normal, cameraPos - pos);

                    var forward = -Vector3.UnitZ.ToDirection(curWorld);
                    var up = Vector3.UnitY.ToDirection(curWorld);

                    var refPos = cameraPos - 2 * distance * normal;

                    var refView = Matrix4x4.CreateLookAt(
                        refPos,
                        refPos + Vector3.Reflect(forward, normal),
                        Vector3.Reflect(up, normal)
                    );

                    if (ReflectionCamera.Eyes == null)
                        ReflectionCamera.Eyes = new CameraEye[2];
                    
                    Matrix4x4.Invert(refView, out var world);

                    ReflectionCamera.Eyes[i].World = world;
                    ReflectionCamera.Eyes[i].Projection = ReflectionCamera.Projection;
                    ReflectionCamera.Eyes[i].View = refView;
                    ReflectionCamera.Eyes[i].ViewProj = refView * ReflectionCamera.Projection;
                }
            }
            else if (boundEye != -1)
            {
                var eye = camera.Eyes![boundEye];

                var cameraPos = eye.World.Translation;
                var forward = -Vector3.UnitZ.ToDirection(eye.World);
                var up = Vector3.UnitY.ToDirection(eye.World);

                var distance = Vector3.Dot(normal, cameraPos - pos);

                var refPos = cameraPos - 2 * distance * normal;

                var refView = Matrix4x4.CreateLookAt(
                    refPos,
                    refPos + Vector3.Reflect(forward, normal),
                    Vector3.Reflect(up, normal)
                );

         
                ReflectionCamera.View = refView;

                if (ReflectionCamera.Eyes == null)
                    ReflectionCamera.Eyes = new CameraEye[2];

                ReflectionCamera.Eyes[boundEye].World = ReflectionCamera.WorldMatrix;
                ReflectionCamera.Eyes[boundEye].Projection = ReflectionCamera.Projection;
                ReflectionCamera.Eyes[boundEye].View = refView;
                ReflectionCamera.Eyes[boundEye].ViewProj = refView * ReflectionCamera.Projection;
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
            }

            if (AutoAdjustFov)
                AdjustFov();

            if (_host?.Scene != null)
                _lightIntensity = _host.Scene.Descendants<Light>().Visible().Select(a => a.Intensity).Sum();
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            return;
            var bounds = _host!.WorldBounds;
            var pos = bounds.Center;
            canvas.Save();
            canvas.State.Color = "#00ff00";
            canvas.DrawPlane(_plane, pos, 1, 2, 0.1f);
            canvas.DrawLine(pos, pos + _plane.Normal);
            canvas.Restore();

        }

        [Range(-1, 1, 0.001f)]
        public float Offset { get; set; }

        public bool RenderEnvironment { get; set; }

        public PerspectiveCamera ReflectionCamera { get; }

        public Texture2D? Texture { get; set; }

        public ShaderMaterial? MaterialOverride { get; set; }

        [Range(1, 180, 1)]
        public float FovDegree { get; set; }

        public uint TextureSize { get; set; }

        public bool AutoAdjustFov { get; set; }

        public bool UseClipPlane { get; set; }  

        public bool AdjustIbl { get; set; }

        public bool UseSrgb { get; set; }   

        public Plane Plane => _plane;

        public PlanarReflectionMode Mode => _mode;

        public static bool IsMultiView { get; set; }

    }
}
