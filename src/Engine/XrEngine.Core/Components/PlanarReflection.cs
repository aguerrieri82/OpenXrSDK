using System.Diagnostics;
using System.Numerics;
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
        private readonly PlanarReflectionMode _mode;
        private readonly PerspectiveCamera _refCamera;
        private Bounds3 _clipBounds;
        private Size2I _renderSize;

        public PlanarReflection()
            : this(1024)
        {
        }

        public PlanarReflection(uint textureSize, PlanarReflectionMode mode = PlanarReflectionMode.ColorOnly)
        {
            _mode = mode;

            _refCamera = new PerspectiveCamera()
            {
                Name = "ReflectionCamera"
            };

            AdjustIbl = false;
            TextureSize = textureSize;
            Offset = 0.01f;
            FovDegree = 45;
            Strength = 1f;
            Roughness = 0;
            ShadingRate = 2;
            Quality = 1f;
            DynamicResolution = true;

            if (_mode == PlanarReflectionMode.ColorOnly)
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

        }

        public virtual bool PrepareMaterial(Material material)
        {
            if (MaterialOverride == null)
                return false;

            MaterialOverride.UseClipDistance = UseClipPlane;

            if (material is ShaderMaterial mat)
                MaterialOverride.UseSkin = mat.UseSkin;

            if (material is IPbrMaterial pbr)
            {
                if (pbr.Alpha == AlphaMode.BlendMain || pbr.Alpha == AlphaMode.Blend)
                    return false;

                if (MaterialOverride is TextureMaterial tex)
                {
                    var texChanged = (tex.Texture == null && pbr.ColorMap != null ||
                                      tex.Texture != null && pbr.ColorMap == null);

                    tex.Texture = pbr.ColorMap;
                    tex.Color = pbr.Color * MathF.Min(1, _lightIntensity * 0.5f);

                    if (texChanged)
                        tex.NotifyChanged(ChangeType.Render);
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
                        bsc.NotifyChanged(ChangeType.Render);

                }

                MaterialOverride.DoubleSided = pbr.DoubleSided;
                MaterialOverride.Alpha = pbr.Alpha;
                return true;
            }

            return false;
        }

        protected void AdjustFov()
        {
            var scaleRatio = 2.0f / MathF.Max(_clipBounds.Size.X, _clipBounds.Size.Y);

            if (scaleRatio < 1)
                return;

            var newFovRadians = 2.0f * MathF.Atan(MathF.Tan(FovDegree / 2.0f) / scaleRatio);

            if (float.IsNaN(newFovRadians))
                return;

            _refCamera.FovDegree = newFovRadians * 180.0f / MathF.PI;

            var newBounds = _host.WorldBounds.Points.Select(_refCamera.Project).ComputeBounds();

            var proj = _refCamera.Projection;

            var regionDistance = Math.Abs(newBounds.Center.Z);

            proj.M31 = newBounds.Center.X / regionDistance;
            proj.M32 = newBounds.Center.Y / regionDistance;

            _refCamera.Projection = proj;

            if (_refCamera.Eyes != null && _refCamera.Eyes.Length == 2)
            {
                _refCamera.Eyes[0].Projection = proj;
                _refCamera.Eyes[0].ViewProj = _refCamera.Eyes[0].View * proj;
                _refCamera.Eyes[0].ViewProjInv = _refCamera.Eyes[0].ViewProj.Invert();

                _refCamera.Eyes[1].Projection = proj;
                _refCamera.Eyes[1].ViewProj = _refCamera.Eyes[1].View * proj;
                _refCamera.Eyes[1].ViewProjInv = _refCamera.Eyes[1].ViewProj.Invert();
            }
        }

        public void Update(Camera mainCamera, int boundEye)
        {
            if (_host == null)
                return;

            var normal = _host.Forward.Normalize();

            _host.UpdateBounds(true);

            var pos = _host.WorldBounds.Center;

            _plane = new Plane(normal, -Vector3.Dot(normal, pos) + Offset);

            _refCamera.Far = mainCamera.Far;
            _refCamera.Near = mainCamera.Near;
            _refCamera.BackgroundColor = mainCamera.BackgroundColor;

            var ratio = mainCamera.ViewSize.Width / (float)mainCamera.ViewSize.Height;

            var curSize = new Size2I
            {
                Width = TextureSize,
                Height = (uint)(TextureSize / ratio)
            };

            if (Texture == null || curSize.Width != Texture.Width || curSize.Height != Texture.Height)
            {
                Texture?.Dispose();

                Texture = new Texture2D
                {
                    MagFilter = ScaleFilter.Linear,
                    MinFilter = ScaleFilter.Linear,
                    WrapS = Wrap,
                    WrapT = Wrap,
                    BorderColor = Color.White,
                    Depth = IsMultiView ? 2u : 1u,
                    MipLevelCount = 0,
                    Width = curSize.Width,
                    Height = curSize.Height,
                    Format = UseSrgb ? TextureFormat.SRgba8 : TextureFormat.Rgba8,
                    Name = "Planar Reflection"
                };
            }

            _refCamera.SetFov(FovDegree, Texture.Width, Texture.Height);

            if (IsMultiView)
            {
                Debug.Assert(mainCamera.Eyes != null && mainCamera.Eyes.Length == 2);

                _refCamera.Eyes ??= new CameraEye[2];

                for (var i = 0; i < 2; i++)
                {
                    var curWorld = mainCamera.Eyes[i].World;

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

                    _refCamera.Eyes[i].World = refView.Invert();
                    _refCamera.Eyes[i].Projection = _refCamera.Projection;
                    _refCamera.Eyes[i].View = refView;
                    _refCamera.Eyes[i].ViewProj = refView * _refCamera.Projection;
                    _refCamera.Eyes[i].ViewProjInv = _refCamera.Eyes[i].ViewProj.Invert();
                }

                _refCamera.WorldMatrix = _refCamera.Eyes[0].World.InterpolateWorldMatrix(_refCamera.Eyes[1].World, 0.5f);
            }
            else
            {
                var cameraPos = mainCamera.WorldPosition;

                var distance = Vector3.Dot(normal, cameraPos - pos);

                var refPos = cameraPos - 2 * distance * normal;

                var refView = Matrix4x4.CreateLookAt(
                    refPos,
                    refPos + Vector3.Reflect(mainCamera.Forward, normal),
                    Vector3.Reflect(mainCamera.Up, normal)
                );

                _refCamera.View = refView;
            }

            _host.UpdateBounds();

            if (DynamicFov)
                _refCamera.FovDegree = ((PerspectiveCamera)mainCamera).FovDegree;
            else
                _refCamera.FovDegree = FovDegree;

            _clipBounds = _host.WorldBounds.Points.Select(_refCamera.Project).ComputeBounds();

            if (AutoAdjustFov)
                AdjustFov();

            if (_host.Scene != null)
                _lightIntensity = _host.Scene.Descendants<Light>().Visible().Sum(a => a.Intensity);

            if (DynamicResolution)
            {
                Bounds3 scaleClipBounds;

                if (_refCamera.FovDegree == ((PerspectiveCamera)mainCamera).FovDegree)
                    scaleClipBounds = _clipBounds;
                else
                    scaleClipBounds = _host.WorldBounds.Points.Select(mainCamera.Project).ComputeBounds();

                var width = Math.Clamp(Math.Min(scaleClipBounds.Max.X, 1) - Math.Max(scaleClipBounds.Min.X, -1), 0, 2) * 0.5f;
                var height = Math.Clamp(Math.Min(scaleClipBounds.Max.Y, 1) - Math.Max(scaleClipBounds.Min.Y, -1), 0, 2) * 0.5f;

                var pixelWidth = width * mainCamera.ViewSize.Width;
                var pixelHeight = height * mainCamera.ViewSize.Height;

                var scaleX = pixelWidth / curSize.Width;
                var scaleY = pixelHeight / curSize.Height;

                var maxScale = Math.Max(scaleX, scaleY);
                var areaScale = MathF.Sqrt(scaleX * scaleY);

                var scale = Math.Min(1.0f, (maxScale + (areaScale - maxScale) * 0.25f) * Quality);

                _renderSize = new Size2I(
                    (uint)Math.Ceiling(curSize.Width * scale),
                    (uint)Math.Ceiling(curSize.Height * scale));

            }
            else
                _renderSize = curSize;
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            /*
            var bounds = _host.WorldBounds;
            var pos = bounds.Center;
            canvas.Save();
            canvas.State.Color = "#00ff00";
            canvas.DrawPlane(_plane, pos, 1, 2, 0.1f);
            canvas.DrawLine(pos, pos + _plane.Normal);
            canvas.Restore();
            */
        }

        protected void NotifyMaterials()
        {
            if (_host is TriangleMesh mesh)
            {
                foreach (var mat in mesh.Materials)
                    mat.NotifyChanged(ChangeType.Render);
            }
        }

        protected override void OnEnabled()
        {
            NotifyMaterials();
        }

        protected override void OnDisabled()
        {
            NotifyMaterials();
        }

        [Action]
        public void Apply()
        {
            NotifyMaterials();
        }

        [Range(-1, 1, 0.001f)]
        public float Offset { get; set; }

        [Range(1, 180, 1)]
        public float FovDegree { get; set; }

        public Texture2D? ActiveTexture { get; set; }

        public Texture2D? Texture { get; internal set; }

        public ShaderMaterial? MaterialOverride { get; set; }

        public uint TextureSize { get; set; }

        public Size2I RenderSize => _renderSize;

        public WrapMode Wrap { get; set; }

        public bool AutoAdjustFov { get; set; }

        public bool UseClipPlane { get; set; }

        public bool AdjustIbl { get; set; }

        public bool UseSrgb { get; set; }

        public bool RenderEnvironment { get; set; }

        public Plane Plane => _plane;

        public Bounds3 ClipBounds => _clipBounds;

        public PlanarReflectionMode Mode => _mode;

        public PerspectiveCamera ReflectionCamera => _refCamera;

        [Range(0, 1, 0.01f)]
        public float Quality { get; set; }

        public bool DynamicFov { get; set; }

        public static bool IsMultiView { get; set; }

        [Range(0, 1, 0.01f)]
        public float Strength { get; set; }

        [Range(0, 1, 0.01f)]
        public float Roughness { get; set; }

        public int ShadingRate { get; set; }

        public bool DynamicResolution { get; set; }
    }
}
