using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseSingleMaterialPass : GlBaseRenderPass
    {
        private GlProgramInstance? _programInstance;

        public GlBaseSingleMaterialPass(OpenGLRender renderer)
            : base(renderer)
        {
        }   

        protected abstract ShaderMaterial CreateMaterial();

        protected override void Initialize()
        {
            var material = CreateMaterial();    

            var global = material.Shader!.GetGlResource(gl => new GlProgramGlobal(_renderer.GL, material.GetType()));

            _programInstance = new GlProgramInstance(_renderer.GL, material, global);
        }


        protected virtual void Draw(DrawContent draw)
        {
            draw.Draw!();
        }

        protected override bool BeginRender()
        {
            Debug.Assert(_programInstance != null);

            var gl = _renderer.GL;

            var updateContext = _renderer.UpdateContext;

            updateContext.Shader = _programInstance.Material.Shader;

            _programInstance.Global!.UpdateProgram(updateContext, _renderer.RenderTarget as IShaderHandler);

            _programInstance.UpdateProgram(updateContext);

            bool programChanged = updateContext.ProgramInstanceId != _programInstance.Program!.Handle;

            updateContext.ProgramInstanceId = _programInstance.Program!.Handle;

            _renderer.State.SetActiveProgram(_programInstance.Program!.Handle);

            if (programChanged)
                _programInstance.Global.UpdateUniforms(updateContext, _programInstance.Program);

            _renderer.ConfigureCaps(_programInstance.Material);

            return true;    
        }


        protected override void RenderLayer(GlLayer layer)
        {

            var updateContext = _renderer.UpdateContext;

            foreach (var shader in layer.Content.ShaderContents)
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

                        _programInstance!.UpdateUniforms(updateContext, false);
                        
                        Draw(draw); 

                    }

                    vHandler.Unbind();
                }
            }
        }

        protected override void EndRender()
        {
            _renderer.State.SetActiveProgram(0);
        }   
    }
}
