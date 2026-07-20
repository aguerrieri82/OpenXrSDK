#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrEngine.Compression;

namespace XrEngine.OpenGL
{
    public static class GlExtensions
    {
        const TextureTarget GL_TEXTURE_EXTERNAL_OES = (TextureTarget)0x8D65;
        const GLEnum GL_TEXTURE_BINDING_EXTERNAL_OES = (GLEnum)0x8D67;

        public static bool CheckError(this GL gl, bool log = true)
        {
            GLEnum err;

            var hasError = false;

            while ((err = gl.GetError()) != GLEnum.NoError)
            {
                if (log)
                    Log.Warn("CheckError", err.ToString());

                hasError = true;
            }

            return hasError;
        }

        public static TextureTarget GetTextureTarget(this GL gL, uint texId)
        {
            TextureTarget[] targets =
            [
                TextureTarget.Texture2DMultisample,
                TextureTarget.Texture2D,
                TextureTarget.Texture2DMultisampleArray,
                TextureTarget.Texture2DArray,
                TextureTarget.TextureCubeMap,
                GL_TEXTURE_EXTERNAL_OES
            ];

            GetPName[] bindings =
            [
                GetPName.TextureBinding2DMultisample,
                GetPName.TextureBinding2D,
                GetPName.TextureBinding2DMultisampleArray,
                GetPName.TextureBinding2DArray,
                GetPName.TextureBindingCubeMap,
                (GetPName)GL_TEXTURE_BINDING_EXTERNAL_OES
            ];

            OpenGLRender.SuspendErrors++;

            try
            {
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];

                    GlState.Current!.LoadTexture(texId, target, 0);

                    gL.GetInteger(bindings[i], out var curTexId);

                    GlState.Current!.BindTexture(target, 0);

                    gL.CheckError(false);

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
                    return texture2D.CreateGlTexture(renderer.GL);

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

            if (texture2D.Compression != TextureCompressionFormat.Uncompressed)
                return null;

            var curSize = texture2D.Width * texture2D.Height;

            if (texture2D is Texture3D tex3d)
                curSize *= tex3d.Depth;

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
            {
                var blockSize = options.BlockSize;

                if (texture2D.Depth <= 1 && blockSize == 3)
                    blockSize = 4;

                return TextureCompressor.EncodeAstc(texture2D.Type == TextureType.NormalMap, options.Quality, blockSize);
            }

            if (options.Format == TextureCompressionFormat.Etc2)
                return TextureCompressor.EncodeEtc2();

            throw new NotSupportedException();
        }

        public static async Task CompressAsync(GlTexture glTexture, Texture2D texture2D, TextureCompressionInfo info)
        {
            var render = OpenGLRender.Current!;
            var sourceVersion = texture2D.Version;
            var newData = new List<TextureData>();
            var curData = texture2D.Data!;

            glTexture.Version = sourceVersion;

            texture2D.NotifyLoaded();

            var compressor = TextureCompressor.Instance;

            compressor.CachePath ??= Path.Combine(Context.Require<IPlatform>().SharedPath, "Cache", "Textures");

            await Task.Run(async () =>
            {
                var groups = curData.GroupBy(a => a.Layer);

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

            await render.Dispatcher.Switch;

            if (glTexture.Source != texture2D || glTexture.Version != sourceVersion)
                return;

            glTexture.UploadFull(
                texture2D.Width,
                texture2D.Height,
                texture2D.Depth,
                newData[0].Format,
                newData[0].Compression,
                newData,
                newData[0].BlockSize);
        }

        public static void Update(this GlTexture glTexture, Texture2D texture2D)
        {
#if GLES
            //Necessary for generate mips

            if (texture2D.Format == TextureFormat.SRgb24 &&
                texture2D.MipLevelCount > 1 &&
                texture2D.Data != null &&
                texture2D.Data.Count == 1)
            {
                texture2D.Format = TextureFormat.SRgba32;
                texture2D.Data[0] = ImageUtils.PackToRgba8(texture2D.Data[0], 1);
            }

#endif
            glTexture.ApplyDescription(texture2D);

            if (texture2D.Type == TextureType.Depth)
            {
                var depth = OpenGLRender.Current?.RenderTarget?.QueryTexture(FramebufferAttachment.DepthAttachment);

                if (depth == null)
                    throw new NotSupportedException();

                glTexture.Attach(depth.Handle, depth.Target);
                return;
            }

            if (texture2D.Type == TextureType.External)
            {
                glTexture.Bind();
                glTexture.UpdateSampler();
                glTexture.Unbind();
                return;
            }

            if (texture2D.Data != null)
            {
                var compInfo = ShouldCompress(texture2D);

                if (compInfo != null)
                {
                    _ = CompressAsync(glTexture, texture2D, compInfo.Value);
                    return;
                }

                glTexture.UploadFull(
                    texture2D.Width,
                    texture2D.Height,
                    texture2D.Depth,
                    texture2D.Format,
                    texture2D.Compression,
                    texture2D.Data,
                    0);

                texture2D.NotifyLoaded();
                return;
            }

            glTexture.Allocate(
                texture2D.Width,
                texture2D.Height,
                texture2D.Depth,
                texture2D.Format,
                texture2D.Compression);
        }

        static void ApplyDescription(this GlTexture glTexture, Texture2D texture2D)
        {
            glTexture.EnableDebug = (texture2D.Flags & EngineObjectFlags.EnableDebug) != 0;

            glTexture.SetTarget(GetTarget(texture2D));

            glTexture.MinFilter = (TextureMinFilter)texture2D.MinFilter;
            glTexture.MagFilter = (TextureMagFilter)texture2D.MagFilter;
            glTexture.WrapS = (TextureWrapMode)texture2D.WrapS;
            glTexture.WrapT = (TextureWrapMode)texture2D.WrapT;
            glTexture.SampleCount = texture2D.SampleCount;
            glTexture.BorderColor = texture2D.BorderColor;
            glTexture.IsMutable = (texture2D.Flags & EngineObjectFlags.Mutable) != 0;
            glTexture.MaxAnisotropy = texture2D.MaxAnisotropy;
            glTexture.MaxLevel = GetMaxLevel(texture2D);

            if (string.IsNullOrWhiteSpace(glTexture.Label))
                glTexture.SetLabel((texture2D.Name ?? "Texture") + " " + glTexture.Handle);

            glTexture.Version = texture2D.Version;
            glTexture.Source = texture2D;

            texture2D.Handle = glTexture.Handle;

            if (texture2D is Texture3D tex3d)
            {
                glTexture.WrapR = (TextureWrapMode)tex3d.WrapR;
            }
        }

        static TextureTarget GetTarget(Texture2D texture2D)
        {
            if (texture2D is Texture3D)
                return TextureTarget.Texture3D;

            if (texture2D is TextureCube)
                return TextureTarget.TextureCubeMap;

            if (texture2D.Type == TextureType.External)
                return GL_TEXTURE_EXTERNAL_OES;

            if (texture2D.Depth > 1)
            {
                if (texture2D.SampleCount > 1)
                    return TextureTarget.Texture2DMultisampleArray;

                return TextureTarget.Texture2DArray;
            }

            if (texture2D.SampleCount > 1)
                return TextureTarget.Texture2DMultisample;

            return TextureTarget.Texture2D;
        }

        static uint GetMaxLevel(Texture2D texture2D)
        {
            if (texture2D.MipLevelCount > 0)
                return texture2D.MipLevelCount - 1;

            if (texture2D.MinFilter == ScaleFilter.LinearMipmapLinear)
            {
                if (texture2D.Width == 0 || texture2D.Height == 0)
                    return 0;

                return (uint)MathF.Floor(MathF.Log2(MathF.Max(texture2D.Width, texture2D.Height)));
            }

            return 0;
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

        internal static Texture2D ToEngineTexture(this GlTexture glTexture, Texture2D result, TextureFormat? readFormat = null)
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
            result.MipLevelCount = glTexture.MaxLevel > 0 ? glTexture.MaxLevel + 1 : 0;

            if (result is Texture3D tex3d)
                tex3d.WrapR = (WrapMode)glTexture.WrapR;

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
            var result = new GlTexture(self.GL)
            {
                Target = self.Target,
                MinFilter = self.MinFilter,
                MagFilter = self.MagFilter,
                WrapS = self.WrapS,
                WrapT = self.WrapT,
                WrapR = self.WrapR,
                MaxAnisotropy = self.MaxAnisotropy,
                BorderColor = self.BorderColor,
                MaxLevel = self.MaxLevel,
                BaseLevel = self.BaseLevel,
                IsMutable = self.IsMutable,
                SampleCount = self.SampleCount
            };

            var texFormat = GlUtils.GetTextureFormat(self.InternalFormat);

            result.Allocate(
                self.Width,
                self.Height,
                self.Depth,
                texFormat);

            if (includeContent)
                self.CopyTo(result);

            return result;
        }

        [Conditional("DEBUG")]
        public static unsafe void DumpState(this GlTexture texture, string? name = null)
        {
            var gl = texture.GL;
            var target = texture.Target;

            static GLEnum GetTextureBindingEnum(TextureTarget target)
            {
                return target switch
                {
                    TextureTarget.Texture2D => GLEnum.TextureBinding2D,
                    TextureTarget.Texture2DArray => GLEnum.TextureBinding2DArray,
                    TextureTarget.Texture3D => GLEnum.TextureBinding3D,
                    TextureTarget.TextureCubeMap => GLEnum.TextureBindingCubeMap,
                    TextureTarget.Texture2DMultisample => GLEnum.TextureBinding2DMultisample,
                    TextureTarget.Texture2DMultisampleArray => GLEnum.TextureBinding2DMultisampleArray,
                    GL_TEXTURE_EXTERNAL_OES => GL_TEXTURE_BINDING_EXTERNAL_OES,
                    _ => throw new NotSupportedException($"Unsupported texture target: {target}")
                };
            }

            void DumpInt(GLEnum pname, string label)
            {
                gl.GetTexParameter(target, pname, out int value);
                Debug.WriteLine($"{label}: {value}");
            }

            void DumpEnum(GLEnum pname, string label)
            {
                gl.GetTexParameter(target, pname, out int value);
                Debug.WriteLine($"{label}: {(GLEnum)value} ({value})");
            }

            void DumpFloatArray(GLEnum pname, string label, int count)
            {
                var values = stackalloc float[count];
                gl.GetTexParameter(target, pname, values);

                var text = string.Join(", ", Enumerable.Range(0, count).Select(i => values[i].ToString("0.###")));
                Debug.WriteLine($"{label}: {text}");
            }

            void DumpLevel()
            {
                const int level = 0;

                Debug.WriteLine("Level 0:");

                DumpLevelInt(GLEnum.TextureWidth, level, "  WIDTH");
                DumpLevelInt(GLEnum.TextureHeight, level, "  HEIGHT");

                if (target == TextureTarget.Texture2DArray ||
                    target == TextureTarget.Texture3D)
                {
                    DumpLevelInt(GLEnum.TextureDepth, level, "  DEPTH");
                }

                DumpLevelEnum(GLEnum.TextureInternalFormat, level, "  INTERNAL_FORMAT");

                if (target == TextureTarget.Texture2DMultisample ||
                    target == TextureTarget.Texture2DMultisampleArray)
                {
                    DumpLevelInt(GLEnum.TextureSamples, level, "  SAMPLES");
                    DumpLevelInt(GLEnum.TextureFixedSampleLocations, level, "  FIXED_SAMPLE_LOCATIONS");
                }
            }

            void DumpLevelInt(GLEnum pname, int level, string label)
            {
                var value = 0;
                gl.GetTexLevelParameter(target, level, pname, &value);
                Debug.WriteLine($"{label}: {value}");
            }

            void DumpLevelEnum(GLEnum pname, int level, string label)
            {
                var value = 0;
                gl.GetTexLevelParameter(target, level, pname, &value);
                Debug.WriteLine($"{label}: {(GLEnum)value} ({value})");
            }

            var previous = 0;
            gl.GetInteger(GetTextureBindingEnum(target), &previous);

            gl.BindTexture(target, texture);

            Debug.WriteLine("");
            Debug.WriteLine($"--- Texture state {(name != null ? $"[{name}]" : "")} ---");
            Debug.WriteLine($"Handle: {texture}");
            Debug.WriteLine($"Target: {target}");

            DumpFloatArray(GLEnum.TextureBorderColor, "BORDER_COLOR", 4);

            DumpInt(GLEnum.TextureBaseLevel, "BASE_LEVEL");
            DumpInt(GLEnum.TextureMaxLevel, "MAX_LEVEL");

            DumpEnum(GLEnum.TextureMinFilter, "MIN_FILTER");
            DumpEnum(GLEnum.TextureMagFilter, "MAG_FILTER");

            DumpEnum(GLEnum.TextureWrapS, "WRAP_S");
            DumpEnum(GLEnum.TextureWrapT, "WRAP_T");

            if (target == TextureTarget.Texture2DArray ||
                target == TextureTarget.Texture3D)
            {
                DumpEnum(GLEnum.TextureWrapR, "WRAP_R");
            }

            DumpEnum(GLEnum.TextureCompareMode, "COMPARE_MODE");
            DumpEnum(GLEnum.TextureCompareFunc, "COMPARE_FUNC");

            DumpEnum(GLEnum.TextureImmutableFormat, "IMMUTABLE_FORMAT");

            DumpLevel();

            gl.BindTexture(target, (uint)previous);

            var err = gl.GetError();

            if (err != GLEnum.NoError)
                Debug.WriteLine($"GL error after DumpTextureState: {err}");

            Debug.WriteLine("--- End texture state ---");
            Debug.WriteLine("");
        }
    }
}