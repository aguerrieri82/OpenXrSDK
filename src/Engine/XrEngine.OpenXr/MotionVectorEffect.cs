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
            readonly Matrix4x4[] _prevViewProj = new Matrix4x4[2];

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

                if (stage == UpdateShaderStage.Model)
                    return;

                bld.ExecuteAction((ctx, up) =>
                {
                    var camera = ctx.PassCamera;

                    Debug.Assert(camera?.Eyes != null);

                    up.SetUniform("uActiveEye", (uint)camera.ActiveEye);

                    up.SetUniform($"uMatrices.prev.viewProj[0]", _prevViewProj[0]);
                    up.SetUniform($"uMatrices.prev.viewProj[1]", _prevViewProj[1]);

                    up.SetUniform($"uMatrices.current.viewProj[0]", camera.Eyes[0].ViewProj);
                    up.SetUniform($"uMatrices.current.viewProj[1]", camera.Eyes[1].ViewProj);

                    if (camera.ActiveEye == -1)
                    {
                        _prevViewProj[0] = camera.Eyes[0].ViewProj;
                        _prevViewProj[1] = camera.Eyes[1].ViewProj;
                    }
                    else
                        _prevViewProj[camera.ActiveEye] = camera.Eyes[camera.ActiveEye].ViewProj;
                });

            }
        }

        MotionVectorEffect()
            : base()
        {
            _shader = new MotionVectorShader
            {
                FragmentSourceName = "motion_vectors.frag",
                VertexSourceName = "motion_vectors.vert",
                Resolver = str => Embedded.GetString(str)
            };
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
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
