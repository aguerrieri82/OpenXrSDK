using System.Numerics;
using XrMath;

namespace XrEngine
{
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

        public FishReflectionSphereMaterial(Texture2D left, Texture2D right)
            : this()
        {
            LeftTexture = left;
            RightTexture = right;
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

                if (camera.ActiveEye == 0 || RightTexture == null)
                    up.SetUniform("uTexture", LeftTexture!, 0);
                else
                    up.SetUniform("uTexture", RightTexture!, 0);
            });

        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public Texture2D? LeftTexture { get; set; }

        public Texture2D? RightTexture { get; set; }

        public float Radius { get; set; }

        public Vector3 Center { get; set; }
    }
}
