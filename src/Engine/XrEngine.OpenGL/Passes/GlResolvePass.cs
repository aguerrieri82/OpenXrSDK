#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlResolvePass : GlBaseRenderPass
    {

        public GlResolvePass(OpenGLRender renderer)
            : base(renderer)
        {
        }

        public override void Render(RenderContext ctx)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}