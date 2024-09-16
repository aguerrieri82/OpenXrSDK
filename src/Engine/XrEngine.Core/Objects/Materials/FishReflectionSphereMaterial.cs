using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public enum FishReflectionMode
    {
        Mono,
        Stereo,
        Eye
    }

    public class FishReflectionSphereMaterial : ShaderMaterial
    {
        protected Quaternion _lastRotation;

        static readonly Shader SHADER;

        static FishReflectionSphereMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "fish_reflection_sphere.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
            };
        }

        public FishReflectionSphereMaterial()
            : base()
        {
            _shader = SHADER;
            DoubleSided = true;
            Fov = MathF.PI;
            Initialize();
        }

        public FishReflectionSphereMaterial(Texture2D main, FishReflectionMode mode)
            : this()
        {
            LeftMainTexture = main;
            Mode = mode;

        }

        public FishReflectionSphereMaterial(Texture2D left, Texture2D right)
            : this()
        {
            LeftMainTexture = left;
            RightTexture = right;
            Mode = FishReflectionMode.Eye;
            Initialize();
        }

        [MemberNotNull(nameof(TextureCenter), nameof(TextureRadius))]   
        protected void Initialize()
        {

            if (Mode == FishReflectionMode.Stereo)
            {
                TextureRadius = [new Vector2(0.5f, 1.0f), new Vector2(0.5f, 1.0f)];
                TextureCenter = [new Vector2(0.25f, 0.5f), new Vector2(0.75f, 0.5f)];
            }
            else
            {
                TextureRadius = [new Vector2(1f, 1f), new Vector2(1f, 1f)];
                TextureCenter = [new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)];
            }

        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<FishReflectionSphereMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<FishReflectionSphereMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (OperatingSystem.IsAndroid())
            {
                bld.AddExtension("GL_OES_EGL_image_external_essl3");
                bld.AddFeature("EXTERNAL");

                if (LeftMainTexture != null)
                    LeftMainTexture.Type = TextureType.External;

                if (RightTexture != null)
                    RightTexture.Type = TextureType.External;
            }

            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ((PerspectiveCamera)ctx.Camera!);

                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uSphereCenter", SphereCenter);
                up.SetUniform("uSphereRadius", SpherRadius);

                if (_lastRotation != ctx.Model!.Transform.Orientation)
                {
                    _lastRotation = ctx.Model!.Transform.Orientation;
                    var newQuat = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI) * _lastRotation;
                    up.SetUniform("uRotation", newQuat.ToMatrix());
                }


                if (Mode == FishReflectionMode.Eye && camera.ActiveEye == 1)
                    up.SetUniform("uTexture", RightTexture!, 0);
                else
                    up.SetUniform("uTexture", LeftMainTexture!, 0);


                up.SetUniform("uActiveEye", (uint)camera.ActiveEye);
                up.SetUniform("uTexCenter", TextureCenter);
                up.SetUniform("uTexRadius", TextureRadius);
                up.SetUniform("uBorder", Border);
                up.SetUniform("uSurfaceSize", SurfaceSize);
                up.SetUniform("uFov", Fov);
            });

        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public FishReflectionMode Mode { get; set; }

        public Texture2D? LeftMainTexture { get; set; }

        public Texture2D? RightTexture { get; set; }


        [Range(0, 10, 0.1f)]
        public float SpherRadius { get; set; }

        public Vector3 SphereCenter { get; set; }

        [Range(0, 6.28f, 0.01f)]
        public float Fov { get; set; }

        [Range(0, 10, 0.01f)]
        public float Border { get; set; }

        public Vector2 SurfaceSize { get; set; }

        public Vector2[] TextureCenter { get; set; }

        public Vector2[] TextureRadius { get; set; }

    }
}
