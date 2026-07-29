using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrEngine.OpenGL
{
    public interface IGlCompositor
    {
        void AppendTexture(GlTexture texture, Bounds2? bounds = null);
    }
}
