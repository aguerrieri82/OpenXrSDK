using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class MotionVectorEffect : ShaderMaterial
    {
        readonly Dictionary<Object3D, Matrix4x4> _models = [];

        public class MotionVectorShader : Shader, IShaderHandler
        {
          

            public MotionVectorShader()
            {

            }

            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                return false;
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {
                var stage = bld.Context.Stage;

                if (!(stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Shader))
                    return;


                bld.ExecuteAction((ctx, up) =>
                {
                    var camera = ctx.PassCamera;

                    Debug.Assert(camera?.Eyes != null);

                    up.SetUniform("uActiveEye", (uint)camera.ActiveEye);

                    up.SetUniform($"uMatrices.prev.viewProj[0]", PrevViewProj[0]);
                    up.SetUniform($"uMatrices.prev.viewProj[1]", PrevViewProj[1]);

                    up.SetUniform($"uMatrices.current.viewProj[0]", camera.Eyes[0].ViewProj);
                    up.SetUniform($"uMatrices.current.viewProj[1]", camera.Eyes[1].ViewProj);
                });
 
            }

            public Matrix4x4[] PrevViewProj = new Matrix4x4[2];
        }

        MotionVectorEffect()
            : base()
        {
            _shader = new MotionVectorShader
            {
                FragmentSourceName = "motion_vectors.frag",
                VertexSourceName = "motion_vectors.vert",
                Resolver = str => Embedded.GetString<Module>(str)
            };
        }

        public void EndPass(Camera camera)
        {
            Debug.Assert(camera.Eyes != null);

            var shader = (MotionVectorShader)_shader!;

            shader.PrevViewProj[0] = camera.Eyes[0].ViewProj;
            shader.PrevViewProj[1] = camera.Eyes[1].ViewProj;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            SkinVertexShader.UpdateShaderModel(bld, true);

            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.PassCamera;

                if (ctx.Model == null || camera == null)
                    return;

                var word = ctx.Model.WorldMatrix;
                if (!word.IsValid())
                    word = Matrix4x4.Identity;

                if (_models.TryGetValue(ctx.Model, out var prevModel))
                    up.SetUniform("uMatrices.prev.model", prevModel);

                up.SetUniform("uMatrices.current.model", word);

                if (camera.ActiveEye == 1 || camera.ActiveEye == -1)
                    _models[ctx.Model] = word;
            });
        }

        public static readonly MotionVectorEffect Instance = new MotionVectorEffect();

    }
}
