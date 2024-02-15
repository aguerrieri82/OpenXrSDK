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
        public static TextureTarget GetTexture2DTarget(this GL gL, uint texId)
        {
            TextureTarget[] targets = [TextureTarget.Texture2D, TextureTarget.Texture2DArray, TextureTarget.Texture2DMultisample, TextureTarget.Texture2DMultisampleArray];

            foreach (var target in targets)
            {
                gL.BindTexture(target, texId);
                gL.GetTexLevelParameter(target, 0, GetTextureParameter.TextureWidth, out int w);
                gL.BindTexture(target, 0);
                if (w != 0)
                    return target;
            }

            throw new NotSupportedException();
        }

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
            {
                if (value.Type == TextureType.Depth)
                {
                    texture.Attach(OpenGLRender.Current!.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment));
                }
                else
                    texture.Create(value.Width, value.Height, value.Format, value.Compression);
            }


            return texture;
        }
    }
}
