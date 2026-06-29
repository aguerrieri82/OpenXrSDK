#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public static class GlTempAllocator
    {
        static Dictionary<string, GlTextureFrameBuffer> _frameBuffers = [];
        static Dictionary<string, GlTexture> _textures = [];
        static Dictionary<string, GlRenderBuffer> _renderBuffers = [];

        public static GlTextureFrameBuffer FrameBuffer(GL gl, string name = SHARED)
        {
            if (!_frameBuffers.TryGetValue(name, out var result) || result.Handle == 0)
            {
                result = new GlTextureFrameBuffer(gl);
                _frameBuffers[name] = result;
            }
            return result;
        }

        public static GlRenderBuffer StaticRenderBuffer(GL gl, uint width, uint height, TextureFormat format, string id = "static")
        {
            var key = $"{width}x{height}x{format}x{id}";

            if (!_renderBuffers.TryGetValue(key, out var result) || result.Handle == 0)
            {
                result = new GlRenderBuffer(gl);

                result.Update(width, height, 1, format.GetInternalFormat());

                _renderBuffers[key] = result;
            }

            return result;
        }

        public static GlTexture StaticTexture(GL gl, uint width, uint height, uint depth, TextureFormat format, string id = "static")
        {
            var key = $"{width}x{height}x{depth}x{format}x{id}";

            if (!_textures.TryGetValue(key, out var result) || result.Handle == 0)
            {
                result = new GlTexture(gl)
                {
                    IsMutable = false,
                    MaxLevel = 0,
                    Target = depth > 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2D
                };

                if (format.GetInternalFormat().IsDepth())
                {
                    result.MinFilter = TextureMinFilter.Nearest;
                    result.MinFilter = TextureMinFilter.Nearest;
                }

                result.Allocate(width, height, depth, format);

                _textures[key] = result;
            }

            return result;
        }


        public static void Dispose()
        {
            foreach (var item in _frameBuffers)
                item.Value.Dispose();

            _frameBuffers.Clear();

            foreach (var item in _textures)
                item.Value.Dispose();

            _textures.Clear();
        }


        public const string SHARED = "Shared";
    }
}
