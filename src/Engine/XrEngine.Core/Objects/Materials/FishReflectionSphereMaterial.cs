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
                IsLit = false
            };
        }
        public FishReflectionSphereMaterial()
            : base()
        {
            _shader = SHADER;
            DoubleSided = true;
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
            Fov = MathF.PI;
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
            bld.ExecuteAction((ctx, up) =>
            {

                var camera = ((PerspectiveCamera)ctx.Camera!);

                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uCenter", Center);
                up.SetUniform("uRadius", Radius);

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

                if (Mode == FishReflectionMode.Stereo)
                {
                    if (camera.ActiveEye == 0)
                        up.SetUniform("uTexCenter", new Vector2(0.25f, 0.5f));
                    else
                        up.SetUniform("uTexCenter", new Vector2(0.75f, 0.5f));

                    up.SetUniform("uTexRadius", new Vector2(0.5f, 1.0f));
                }
                else
                {
                    up.SetUniform("uTexRadius", new Vector2(1f, 1f));
                    up.SetUniform("uTexCenter", new Vector2(0.5f, 0.5f));
                }

                up.SetUniform("uFov", Fov);
            });

        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public FishReflectionMode Mode  { get; set; }

        public Texture2D? LeftMainTexture { get; set; }

        public Texture2D? RightTexture { get; set; }

        public float Radius { get; set; }

        public Vector3 Center { get; set; }

        [Range(0, 6.28f, 0.01f)]
        public float Fov { get; set; }
    }
}
