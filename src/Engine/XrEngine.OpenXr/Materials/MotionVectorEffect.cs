using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class MotionVectorEffect : ShaderMaterial
    {
        readonly Dictionary<Object3D, Matrix4x4> _models = [];
        readonly Dictionary<Object3D, Matrix4x4[]> _skins = [];
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

            bld.LoadBufferArray(ctx =>
            {
                if (bld.Context.Model is not ISkinnedMesh mesh)
                    return null;

                if (!_skins.TryGetValue(bld.Context.Model, out var matrices))
                    return null;

                return matrices;

            }, 17, BufferStore.Model, BufferUsage.SSbo);


            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.PassCamera;
                var model = ctx.Model;

                if (model == null || camera == null)
                    return;

                var world = model.WorldMatrix;

                if (!world.IsValid())
                    world = Matrix4x4.Identity;

                if (_models.TryGetValue(model, out var prevModel))
                    up.SetUniform("uMatrices.prev.model", prevModel);

                up.SetUniform("uMatrices.current.model", world);

                if (camera.ActiveEye == 1 || camera.ActiveEye == -1)
                {
                    _models[model] = world;

                    if (ctx.Model is ISkinnedMesh skinned)
                    {
                        if (!_skins.TryGetValue(model, out var matrices))
                        {
                            matrices = new Matrix4x4[skinned.SkinMatrices.Length];

                            _skins[model] = matrices;
                        }

                        Array.Copy(skinned.SkinMatrices, matrices, matrices.Length);
                    }
  
                }
            });
        }

        public static readonly MotionVectorEffect Instance = new MotionVectorEffect();

    }
}
