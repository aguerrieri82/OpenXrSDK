#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using XrEngine.Compression;


namespace XrEngine.OpenGL
{
    public static class Extensions
    {
        public static TextureTarget GetTextureTarget(this GL gL, uint texId)
        {
            TextureTarget[] targets = [TextureTarget.Texture2DMultisample, TextureTarget.Texture2D, TextureTarget.Texture2DMultisampleArray, TextureTarget.Texture2DArray, TextureTarget.TextureCubeMap];
            GetPName[] bindings = [GetPName.TextureBinding2DMultisample, GetPName.TextureBinding2D, GetPName.TextureBinding2DMultisampleArray, GetPName.TextureBinding2DArray, GetPName.TextureBindingCubeMap];

            OpenGLRender.SuspendErrors++;

            try
            {
                for (var i = 0; i < targets.Length; i++) 
                {
                    var target = targets[i];    
                    gL.BindTexture(target, texId);
                    gL.GetInteger(bindings[i], out int curTexId);
                    gL.BindTexture(target, 0);
                    if (curTexId == texId)
                        return target;
                }
            }
            finally
            {
                OpenGLRender.SuspendErrors--;
            }

            throw new NotSupportedException();
        }

        public static unsafe TRes GetResource<T, TRes>(this T obj, Func<T, TRes> factory) where T : EngineObject
        {
            var glObj = obj.GetProp<TRes?>(OpenGLRender.Props.GlResId);
            if (glObj == null)
            {
                glObj = factory(obj);
                obj.SetProp(OpenGLRender.Props.GlResId, glObj);
            }

            return glObj;
        }


        public static unsafe void Update(this GlTexture glTexture, Texture2D texture2D)
        {
            glTexture.MinFilter = (TextureMinFilter)texture2D.MinFilter;
            glTexture.MagFilter = (TextureMagFilter)texture2D.MagFilter;
            glTexture.WrapS = (TextureWrapMode)texture2D.WrapS;
            glTexture.WrapT = (TextureWrapMode)texture2D.WrapT;

            glTexture.Update(texture2D.Width, texture2D.Height, texture2D.Format, texture2D.Compression, texture2D.Data);
        }

        public static unsafe Texture TexIdToEngineTexture(this GL gl, uint texId, TextureFormat? readFormat = null)
        {
            return new GlTexture(gl, texId).ToEngineTexture(readFormat);
        }

        public static unsafe Texture ToEngineTexture(this GlTexture glTexture, TextureFormat? readFormat = null)
        {
            Texture2D res;

            if (glTexture.Target == TextureTarget.TextureCubeMap)
                res = new TextureCube();
            else
                res = new Texture2D();

            res.Width = glTexture.Width;
            res.Height= glTexture.Height;
            res.WrapT = (WrapMode)glTexture.WrapT;
            res.WrapS = (WrapMode)glTexture.WrapS;
            res.MagFilter = (ScaleFilter)glTexture.MagFilter;
            res.MinFilter = (ScaleFilter)glTexture.MinFilter;

            switch (glTexture.InternalFormat)
            {
                case InternalFormat.Rgb32f:
                    res.Format = TextureFormat.RgbFloat;
                    break;
            }
            
            res.SetProp(OpenGLRender.Props.GlResId, glTexture);

            if (readFormat != null)
                res.Data = glTexture.Read(readFormat.Value);

            return res;
        }


        //TODO bind GlTexture with Texture2D object

        public static unsafe GlTexture CreateGlTexture(this Texture2D value, GL gl, bool requireCompression)
        {

            var texture = new GlTexture(gl)
            {
                MinFilter = (TextureMinFilter)value.MinFilter,
                MagFilter = (TextureMagFilter)value.MagFilter,
                WrapS = (TextureWrapMode)value.WrapS,
                WrapT = (TextureWrapMode)value.WrapT
            };

            if (value.Data != null)
            {
                var data = value.Data;
                var comp = value.Compression;

                if (requireCompression)
                {
                    if (data.Count == 1)
                        data = EtcCompressor.Encode(data[0], 16);
                    else
                    {
                        for (var i = 0; i < data.Count; i++)
                        {
                            var compData = EtcCompressor.Encode(data[i], 0);
                            data[i] = compData[0];
                        }
                    }

                    comp = TextureCompressionFormat.Etc2;
                }

                texture.Update(value.Width, value.Height, data[0].Format, comp, data);

                value.Data = null;
            }
            else
            {
                if (value.Type == TextureType.Depth)
                    texture.Attach(OpenGLRender.Current!.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment));
                else
                    texture.Update(value.Width, value.Height, value.Format, value.Compression);
            }

            return texture;
        }
    }
}
