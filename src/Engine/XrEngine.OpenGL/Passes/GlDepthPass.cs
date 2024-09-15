#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlDepthPass : GlBaseRenderPass
    {
        GlProgramInstance? _programInstance;


        public GlDepthPass(OpenGLRender renderer)
            : base(renderer)
        {
            UseOcclusion = true;
            IsEnabled = true;
        }

        public override void Initialize()
        {
            var material = new ColorMaterial
            {
                WriteColor = false,
                UseDepth = true,
                WriteDepth = true
            };

            var global = material.Shader!.GetGlResource(gl => new GlProgramGlobal(_renderer.GL, material.GetType()));

            _programInstance = new GlProgramInstance(_renderer.GL, material, global);
        }

        protected override void RenderContentWork(GlobalContent content)
        {
            Debug.Assert(_programInstance != null);

            var gl = _renderer.GL;

            var updateContext = _renderer.UpdateContext;
            updateContext.Shader = _programInstance.Material.Shader;

            _programInstance.Global!.UpdateProgram(updateContext, _renderer.RenderTarget as IShaderHandler);

            _programInstance.UpdateProgram(updateContext);

            updateContext.ProgramInstanceId = _programInstance.Program!.Handle;

            if (_renderer.State.ActiveProgram != _programInstance.Program!.Handle)
            {
                _programInstance.Program!.Use();
                _renderer.State.ActiveProgram = _programInstance.Program!.Handle;
                _programInstance.Global.UpdateUniforms(updateContext, _programInstance.Program!);
            }

            _renderer.ConfigureCaps(_programInstance.Material);

            foreach (var shader in content.ShaderContents)
            {
                foreach (var vertex in shader.Value.Contents)
                {
                    var vHandler = vertex.Value.VertexHandler!;

       

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    updateContext.ActiveComponents = vertex.Value.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Value.Contents)
                    {
                        if (draw.Object is TriangleMesh mesh && _renderer.Options.FrustumCulling)
                        {
                            draw.IsHidden = !mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!);
                            if (draw.IsHidden)
                                continue;
                        }
                        else
                            draw.IsHidden = false;

                        var material = draw.ProgramInstance!.Material;

                        if (!material.WriteDepth)
                            continue;

                        updateContext.Model = draw.Object;

                        _programInstance.UpdateUniforms(updateContext, false);

                        if (UseOcclusion)
                        {
                            draw.Query ??= draw.Object!.GetOrCreateProp(OpenGLRender.Props.GlQuery, () => new GlQuery(gl));

                            draw.Query!.Begin(QueryTarget.AnySamplesPassed);

                            draw.Draw!();

                            draw.Query.End();
                        }
                        else
                            draw.Draw!();

                    }

                    vHandler.Unbind();
                }
            }
        }

        public bool UseOcclusion { get; set; }
    }
}
