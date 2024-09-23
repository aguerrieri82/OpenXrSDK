#if GLES
using Silk.NET.OpenGLES;
using System;


#else
using Silk.NET.OpenGL;
#endif

using XrEngine.Compression;


namespace XrEngine.OpenGL
{
    public static class Extensions
    {
        public static void CheckError(this GL gl)
        {
            GLEnum err;

            while ((err = gl.GetError()) != GLEnum.NoError)
            {
                //throw new Exception(err.ToString());
            }
        }

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
                    
                    GlState.Current!.SetActiveTexture(texId, target, 0);
                    
                    gL.GetInteger(bindings[i], out int curTexId);
                    
                    GlState.Current!.BindTexture(target, 0);

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

        public static unsafe TRes GetGlResource<T, TRes>(this T obj, Func<T, TRes> factory) where T : EngineObject
        {
            return obj.GetOrCreateProp(OpenGLRender.Props.GlResId, () => factory(obj));
        }


        //TODO bind GlTexture with Texture2D object

        public static unsafe GlTexture CreateGlTexture(this Texture2D value, GL gl, bool requireCompression)
        {
            //TODO !!WARN!! change this
            requireCompression = false;

            var glTexture = new GlTexture(gl);
            glTexture.Update(value, requireCompression);
            return glTexture;
        }

        public static unsafe void Update(this GlTexture glTexture, Texture2D texture2D, bool requireCompression)
        {
            glTexture.EnableDebug = (texture2D.Flags & EngineObjectFlags.EnableDebug) != 0;
            
            if (texture2D is TextureCube)
                glTexture.Target = TextureTarget.TextureCubeMap;

            else if (texture2D.Type == TextureType.External)
                glTexture.Target = (TextureTarget)0x8D65;


            glTexture.MinFilter = (TextureMinFilter)texture2D.MinFilter;
            glTexture.MagFilter = (TextureMagFilter)texture2D.MagFilter;
            glTexture.WrapS = (TextureWrapMode)texture2D.WrapS;
            glTexture.WrapT = (TextureWrapMode)texture2D.WrapT;
            glTexture.SampleCount = texture2D.SampleCount;
            glTexture.BorderColor = texture2D.BorderColor;
            glTexture.IsMutable = texture2D.IsMutable;

            if (texture2D.MaxLevels > 0)
                glTexture.MaxLevel = texture2D.MaxLevels - 1;

            if (texture2D.SampleCount > 1)
                glTexture.Target = TextureTarget.Texture2DMultisample;

            if (texture2D.Data != null)
            {
                var data = texture2D.Data;
                var comp = texture2D.Compression;

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

                glTexture.Update(texture2D.Width, texture2D.Height, texture2D.Format, comp, data);
                texture2D.NotifyLoaded();
            }
            else
            {
                if (texture2D.Type == TextureType.Depth)
                    glTexture.Attach(OpenGLRender.Current!.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment));
                else
                    glTexture.Update(texture2D.Width, texture2D.Height, texture2D.Format, texture2D.Compression);
            }

            glTexture.Version = texture2D.Version;
            glTexture.Source = texture2D;   
        }

        public static unsafe Texture TexIdToEngineTexture(this GL gl, uint texId, TextureFormat? readFormat = null)
        {
            return GlTexture.Attach(gl, texId).ToEngineTexture(readFormat);
        }

        public static unsafe Texture ToEngineTexture(this GlTexture glTexture, TextureFormat? readFormat = null)
        {
            if (glTexture.Source is Texture texture)
                return texture;

            Texture2D res;

            if (glTexture.Target == TextureTarget.TextureCubeMap)
                res = new TextureCube();
            else
                res = new Texture2D();

            res.Width = glTexture.Width;
            res.Height = glTexture.Height;
            res.WrapT = (WrapMode)glTexture.WrapT;
            res.WrapS = (WrapMode)glTexture.WrapS;
            res.MagFilter = (ScaleFilter)glTexture.MagFilter;
            res.MinFilter = (ScaleFilter)glTexture.MinFilter;
            res.BorderColor = glTexture.BorderColor;
            res.SampleCount = glTexture.SampleCount;

            res.Format = glTexture.InternalFormat switch
            {
                InternalFormat.Rgb32f => TextureFormat.RgbFloat32,
                InternalFormat.Rgba16f => TextureFormat.RgbaFloat16,
                InternalFormat.Rgba => TextureFormat.Rgba32,
                InternalFormat.Depth24Stencil8 => TextureFormat.Depth24Stencil8,
                InternalFormat.DepthComponent24 => TextureFormat.Depth24Float,
                InternalFormat.Depth32fStencil8 => TextureFormat.Depth32Stencil8,
                _ => throw new NotSupportedException(),
            };

            res.SetProp(OpenGLRender.Props.GlResId, glTexture);

            if (readFormat != null)
                res.Data = glTexture.Read(readFormat.Value);

            glTexture.Source = res;

            return res;
        }
    }
}
