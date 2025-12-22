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
        public static void CheckError(this GL gl)
        {
            GLEnum err;

            while ((err = gl.GetError()) != GLEnum.NoError)
            {
                Log.Warn("CheckError", err.ToString());

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

                    GlState.Current!.LoadTexture(texId, target, 0);

                    gL.GetInteger(bindings[i], out var curTexId);

                    GlState.Current!.BindTexture(target, 0);

                    gL.CheckError();

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

        public static TRes GetGlResource<T, TRes>(this T obj, Func<T, TRes> factory) where T : EngineObject
        {
            return obj.GetOrCreateProp(OpenGLRender.Props.GlResId, () =>
            {
                var result = factory(obj);
                if (result != null)
                    ObjectBinder.Bind(obj, result);
                return result;
            });
        }

        public static GlTexture ToGlTexture(this Texture value)
        {
            return value.GetGlResource(a =>
            {
                var renderer = OpenGLRender.Current!;

                if (value is Texture2D texture2D)
                {
                    if (texture2D.Type == TextureType.Depth)
                    {
                        var glTex = renderer.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment);
                        if (glTex == null)
                            throw new NotSupportedException();
                        if (texture2D.Handle == 0)
                            glTex.ToEngineTexture(texture2D);
                        return glTex;
                    }

                    return texture2D.CreateGlTexture(renderer.GL);
                }

                throw new NotSupportedException();
            });
        }

        static GlTexture CreateGlTexture(this Texture2D value, GL gl)
        {
            GlTexture glTexture;

            if (value.Handle != 0)
            {
                glTexture = GlTexture.Attach(gl, (uint)value.Handle, value.SampleCount);
                glTexture.ToEngineTexture(value);
                return glTexture;
            }

            glTexture = new GlTexture(gl);
            glTexture.Update(value);
            return glTexture;
        }

        public static TextureCompressionInfo? ShouldCompress(Texture2D texture2D)
        {
            var options = OpenGLRender.Current!.Options.Compression;

            if (!options.Use)
                return null;

            var curSize = texture2D.Width * texture2D.Height;
            if (curSize < options.MinSize)
                return null;

            if (texture2D.NeverCompress)
                return null;

            var isFloat = texture2D.Format.IsFloat();

            if (isFloat && options.Format == TextureCompressionFormat.Etc2)
                return null;

            if (texture2D.Data == null || !texture2D.Data.All(a => a.Data != null))
                return null;

            if (options.Format == TextureCompressionFormat.Astc)
                return TextureCompressor.EncodeAstc(texture2D.Type == TextureType.NormalMap, options.Quality, options.BlockSize);

            if (options.Format == TextureCompressionFormat.Etc2)
                return TextureCompressor.EncodeEtc2();

            throw new NotSupportedException();
        }

        public static async Task CompressAsync(GlTexture glTexture, Texture2D texture2D, TextureCompressionInfo info)
        {
            var render = OpenGLRender.Current!;

            var newData = new List<TextureData>();

            var curData = texture2D.Data!;

            glTexture.Version = texture2D.Version;

            texture2D.NotifyLoaded();

            var compressor = TextureCompressor.Instance;

            compressor.CachePath ??= Path.Combine(Context.Require<IPlatform>().CachePath, "Textures");

            //compressor.ClearCache();

            await Task.Run(async () =>
            {
                var groups = curData.GroupBy(a => new
                {
                    a.Face,
                    a.Depth
                });

                foreach (var dataGrp in groups)
                {
                    var mipLevels = 0;

                    if (glTexture.MaxLevel > 0 && dataGrp.Count() == 1)
                        mipLevels = (int)glTexture.MaxLevel + 1;

                    foreach (var item in dataGrp)
                    {
                        var compData = await compressor.EncodeAsync(item, mipLevels, info);

                        newData.AddRange(compData);
                    }
                }

            });

            await render.Dispatcher.ExecuteAsync(() =>
            {
                glTexture.Update(texture2D.Width, texture2D.Height, texture2D.Depth,
                    newData[0].Format,
                    newData[0].Compression,
                    newData,
                    newData[0].BlockSize);
            });
        }

        public static void Update(this GlTexture glTexture, Texture2D texture2D)
        {
            glTexture.EnableDebug = (texture2D.Flags & EngineObjectFlags.EnableDebug) != 0;

            if (texture2D is TextureCube)
                glTexture.Target = TextureTarget.TextureCubeMap;

            else if (texture2D.Type == TextureType.External)
                glTexture.Target = (TextureTarget)0x8D65;

            else if (texture2D.Depth > 1)
            {
                if (texture2D.SampleCount > 1)
                    glTexture.Target = TextureTarget.Texture2DMultisampleArray;
                else
                    glTexture.Target = TextureTarget.Texture2DArray;
            }
            else
            {
                if (texture2D.SampleCount > 1)
                    glTexture.Target = TextureTarget.Texture2DMultisample;
                else
                    glTexture.Target = TextureTarget.Texture2D;
            }

            glTexture.MinFilter = (TextureMinFilter)texture2D.MinFilter;
            glTexture.MagFilter = (TextureMagFilter)texture2D.MagFilter;
            glTexture.WrapS = (TextureWrapMode)texture2D.WrapS;
            glTexture.WrapT = (TextureWrapMode)texture2D.WrapT;
            glTexture.SampleCount = texture2D.SampleCount;
            glTexture.BorderColor = texture2D.BorderColor;
            glTexture.IsMutable = (texture2D.Flags & EngineObjectFlags.Mutable) != 0;
            glTexture.MaxAnisotropy = texture2D.MaxAnisotropy;

            if (string.IsNullOrWhiteSpace(glTexture.Label))
                glTexture.SetLabel((texture2D.Name ?? "Texture") + " " + glTexture.Handle);

            if (texture2D.MinFilter == ScaleFilter.LinearMipmapLinear)
                glTexture.MaxLevel = (uint)MathF.Log2(MathF.Max(texture2D.Width, texture2D.Height));

            else if (texture2D.MipLevelCount > 0)
                glTexture.MaxLevel = texture2D.MipLevelCount - 1;


            glTexture.Version = texture2D.Version;
            glTexture.Source = texture2D;
            texture2D.Handle = glTexture.Handle;

            if (texture2D.Data != null)
            {
                var compInfo = ShouldCompress(texture2D);

                if (compInfo != null)
                {
                    _ = CompressAsync(glTexture, texture2D, compInfo.Value);
                }
                else
                {
                    glTexture.Update(texture2D.Width,
                        texture2D.Height,
                        texture2D.Depth,
                        texture2D.Format,
                        TextureCompressionFormat.Uncompressed,
                        texture2D.Data,
                        0);

                    texture2D.NotifyLoaded();
                }
            }
            else
            {
                if (texture2D.Type == TextureType.Depth)
                    glTexture.Attach(OpenGLRender.Current!.RenderTarget!.QueryTexture(FramebufferAttachment.DepthAttachment)!);
                else
                    glTexture.Update(texture2D.Width, texture2D.Height, texture2D.Depth, texture2D.Format, texture2D.Compression);
            }

        }

        public static Texture TexIdToEngineTexture(this GL gl, uint texId, TextureFormat? readFormat = null)
        {
            return GlTexture.Attach(gl, texId).ToEngineTexture(readFormat);
        }

        public static Texture ToEngineTexture(this GlTexture glTexture, TextureFormat? readFormat = null)
        {
            if (glTexture.Source is Texture texture)
                return texture;

            Texture2D result;

            if (glTexture.Target == TextureTarget.TextureCubeMap)
                result = new TextureCube();
            else
                result = new Texture2D();

            glTexture.ToEngineTexture(result, readFormat);

            glTexture.Source = result;

            return result;

        }
        public static Texture ToEngineTexture(this GlTexture glTexture, Texture2D result, TextureFormat? readFormat = null)
        {
            result.Width = glTexture.Width;
            result.Height = glTexture.Height;
            result.Depth = glTexture.Depth;
            result.WrapT = (WrapMode)glTexture.WrapT;
            result.WrapS = (WrapMode)glTexture.WrapS;
            result.MagFilter = (ScaleFilter)glTexture.MagFilter;
            result.MinFilter = (ScaleFilter)glTexture.MinFilter;
            result.BorderColor = glTexture.BorderColor;
            result.SampleCount = glTexture.SampleCount;
            result.MaxAnisotropy = glTexture.MaxAnisotropy;
            result.Handle = glTexture.Handle;
            result.Format = GlUtils.GetTextureFormat(glTexture.InternalFormat);

            if (glTexture.IsMutable)
                result.Flags |= EngineObjectFlags.Mutable;

            result.SetProp(OpenGLRender.Props.GlResId, glTexture);

            if (readFormat != null)
                result.Data = glTexture.Read(readFormat.Value);

            return result;
        }

        public static T? Pass<T>(this OpenGLRender self) where T : IGlRenderPass
        {
            return self.Passes<T>().Single();
        }

        public static bool HasPass<T>(this OpenGLRender self) where T : IGlRenderPass
        {
            return self.Passes<T>().Any();
        }

        public static GlTexture Clone(this GlTexture self, bool includeContent)
        {
            var result = new GlTexture(self.GL);

            result.Target = self.Target;
            result.MinFilter = self.MinFilter;
            result.MagFilter = self.MagFilter;
            result.WrapS = self.WrapS;
            result.WrapT = self.WrapT;
            result.MaxAnisotropy = self.MaxAnisotropy;
            result.BorderColor = self.BorderColor;
            result.MaxLevel = self.MaxLevel;
            result.BaseLevel = self.BaseLevel;
            result.IsMutable = self.IsMutable;

            var texFormat = GlUtils.GetTextureFormat(self.InternalFormat);

            result.Update(1, new TextureData
            {
                Width = self.Width,
                Height = self.Height,
                Depth = self.Depth,
                Format = texFormat
            });

            return result;

        }

    }
}
