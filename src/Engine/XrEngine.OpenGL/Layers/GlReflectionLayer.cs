using XrMath;

namespace XrEngine.OpenGL
{

    public class GlReflectionLayer : GlLayer
    {
        public GlReflectionLayer(OpenGLRender render, Scene3D scene)
            : base(render, scene, GlLayerType.FullReflection)   
        {
        }

        public override void Update()
        {
            base.Update();

            var main = _render.Layers.First(a => a.Type == GlLayerType.Main);
            _content.Lights = main.Content.Lights;
            _content.LightsHash = main.Content.LightsHash;
        }
    }
}
