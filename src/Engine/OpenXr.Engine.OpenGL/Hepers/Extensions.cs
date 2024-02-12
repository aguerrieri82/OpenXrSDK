#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using static OpenXr.Engine.OpenGL.OpenGLRender;

namespace OpenXr.Engine.OpenGL
{
    public static class Extensions
    {
        public static unsafe TGl GetResource<T, TGl>(this T obj, Func<T, TGl> factory) where T : EngineObject where TGl : GlObject
        {
            var glObj = obj.GetProp<TGl?>(Props.GlResId);
            if (glObj == null)
            {
                glObj = factory(obj);
                obj.SetProp(Props.GlResId, glObj);
            }

            return glObj;
        }

        public static unsafe GlTexture2D CreateGlTexture(this Texture2D value, GL gl)
        {
            var texture = new GlTexture2D(gl)
            {
                MinFilter = (TextureMinFilter)value.MinFilter,
                MagFilter = (TextureMagFilter)value.MagFilter,
                WrapS = (TextureWrapMode)value.WrapS,
                WrapT = (TextureWrapMode)value.WrapT
            };

            if (value.Data != null)
            {
                texture.Create(value.Width, value.Height, value.Format, value.Compression, value.Data);

                value.Data = null;
            }
            else
                texture.Create(value.Width, value.Height, value.Format, value.Compression);


            return texture;
        }
    }
}
