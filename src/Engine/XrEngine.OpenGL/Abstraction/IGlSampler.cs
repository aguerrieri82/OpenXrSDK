#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public interface IGlSampler
    {
        TextureMinFilter MinFilter { get; set; }

        TextureMagFilter MagFilter { get; set; }

        TextureWrapMode WrapS { get; set; }

        TextureWrapMode WrapT { get; set; }

        TextureWrapMode WrapR { get; set; }

        Color BorderColor { get; set; }

        public float MaxAnisotropy { get; set; }
    }
}
