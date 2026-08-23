#if GLES
using Silk.NET.OpenGLES;
using ExtShaderFramebufferFetchNonCoherent = Silk.NET.OpenGLES.Extensions.QCOM.QComShaderFramebufferFetchNoncoherent;
using ExtFragmentShadingRate = Silk.NET.OpenGLES.Extensions.EXT.ExtFragmentShadingRate;
#else
using Silk.NET.OpenGL;
using ExtFragmentShadingRate = Silk.NET.OpenGL.Extensions.EXT.ExtFragmentShadingRate;
using ExtShaderFramebufferFetchNonCoherent = Silk.NET.OpenGL.Extensions.EXT.ExtShaderFramebufferFetchNonCoherent;

#endif

using System.Diagnostics;
using XrEngine.Compression;
using Common.Interop;
using XrMath;
using System.Numerics;
using static MeshOptimizer.MeshOptimizerLib;


namespace XrEngine.OpenGL
{
    public static class GlExtensions
    {
        public const TextureTarget GL_TEXTURE_EXTERNAL_OES = (TextureTarget)0x8D65;
        public const GLEnum GL_TEXTURE_BINDING_EXTERNAL_OES = (GLEnum)0x8D67;

        static ExtShaderFramebufferFetchNonCoherent? _fbFetchExt;
        static ExtFragmentShadingRate? _fsRateExt;


        #region EngineObject

        extension<T>(T obj) where T : EngineObject
        {
            public TRes GetGlResource<TRes>(Func<T, TRes> factory)
            {
                return obj.GetOrCreateProp(OpenGLRender.Props.GlResId, () =>
                {
                    var result = factory(obj);

                    if (result != null)
                        ObjectBinder.Bind(obj, result);

                    return result;
                });
            }
        }

        #endregion

        #region GlBuffer<T>

        extension<T>(GlBuffer<T> self)
        {
            public unsafe void Update(IMemoryBuffer<byte> data)
            {
                using var pDst = self.Map(MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

                using var pSrc = data.MemoryLock();

                EngineNativeLib.CopyMemory(pSrc, (nint)pDst.Data, data.Size);
            }

            public unsafe T* MapPermanentRead()
            {
                return self.MapPermanent(MapBufferAccessMask.ReadBit);
            }

            public unsafe T* MapPermanentWrite()
            {
                return self.MapPermanent(MapBufferAccessMask.WriteBit);
            }

            public unsafe T* MapPermanent(MapBufferAccessMask access)
            {
                return self.MapRange(0, self.SizeBytes, access | MapBufferAccessMask.PersistentBit | MapBufferAccessMask.CoherentBit);
            }
        }

        #endregion

        #region Texture

        extension(Texture value)
        {
            public GlTexture ToGlTexture()
            {
                return value.GetGlResource(a =>
                {
                    var renderer = OpenGLRender.Current!;

                    if (value is Texture2D texture2D)
                        return texture2D.CreateGlTexture(renderer.GL);

                    throw new NotSupportedException();
                });
            }
        }

        #endregion

        #region Texture2D

        extension(Texture2D texture2D)
        {
            GlTexture CreateGlTexture(GL gl)
            {
                GlTexture glTexture;

                if (texture2D.Handle != 0)
                {
                    glTexture = GlTexture.Attach(gl, (uint)texture2D.Handle, texture2D.SampleCount);
                    glTexture.ToEngineTexture(texture2D);
                    return glTexture;
                }

                glTexture = new GlTexture(gl);
                glTexture.Sampler = texture2D.Sampler?.ToGlSampler();
                glTexture.Update(texture2D);
                return glTexture;
            }

            public TextureCompressionInfo? ShouldCompress()
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

                if (texture2D.Data == null || !texture2D.Data.All(a => a.Content != null))
                    return null;

                if (options.Format == TextureCompressionFormat.Astc)
                {
                    var blockSize = options.BlockSize;

                    if (texture2D.Depth <= 1 && blockSize == 3)
                        blockSize = 4;

                    var threadPriority = 0;

#if __ANDROID__
                    threadPriority = -2;
#endif
                    return TextureCompressor.EncodeAstc(texture2D.Type == TextureType.NormalMap, options.Quality, blockSize, threadPriority);
                }

                if (options.Format == TextureCompressionFormat.Etc2)
                    return TextureCompressor.EncodeEtc2();

                throw new NotSupportedException();
            }

            TextureTarget GetTarget()
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

            uint GetMaxLevel()
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
        }

        #endregion

        #region TextureSampler

        extension(TextureSampler value)
        {
            public GlSampler ToGlSampler()
            {
                return value.GetGlResource(a =>
                {
                    var renderer = OpenGLRender.Current!;

                    var result = new GlSampler(renderer.GL);
                    result.Update(value);
                    return result;
                });
            }
        }

        #endregion

        #region GlSampler

        extension(GlSampler glSampler)
        {
            public void Update(TextureSampler sampler)
            {
                glSampler.Source = sampler;
                glSampler.Version = sampler.Version;

                glSampler.MinFilter = (TextureMinFilter)sampler.MinFilter;
                glSampler.MagFilter = (TextureMagFilter)sampler.MagFilter;

                glSampler.WrapS = (TextureWrapMode)sampler.WrapS;
                glSampler.WrapT = (TextureWrapMode)sampler.WrapT;
                glSampler.WrapR = (TextureWrapMode)sampler.WrapR;

                glSampler.BorderColor = sampler.BorderColor;
                glSampler.MinLod = sampler.MinLod;
                glSampler.MaxLod = sampler.MaxLod;
                glSampler.LodBias = sampler.LodBias;
                glSampler.MaxAnisotropy = sampler.MaxAnisotropy;
                glSampler.DecodeSrgb = sampler.DecodeSrgb;
                glSampler.CompareMode = sampler.UseTexCompare ? TextureCompareMode.CompareRefToTexture : TextureCompareMode.None;
                glSampler.CompareFunc = (DepthFunction)sampler.CompareFunc;

                glSampler.Update();
            }
        }

        #endregion

        #region GlTexture

        extension(GlTexture glTexture)
        {
            public async Task CompressAsync(Texture2D source, TextureCompressionInfo info)
            {
                const string TAG = nameof(CompressAsync);

                Log.Debug(TAG, "Compressing {0}", glTexture.Handle);

                try
                {
                    var render = OpenGLRender.Current!;
                    var sourceVersion = source.Version;
                    var newData = new List<TextureData>();
                    var curData = source.Data!;

                    glTexture.Version = sourceVersion;

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
                                var compData = await compressor.EncodeAsync(item, mipLevels, info, glTexture.Handle);
                                newData.AddRange(compData);
                            }
                        }
                    });

                    await EngineApp.MainThread;

                    if (glTexture.Source != source || glTexture.Version != sourceVersion)
                    {
                        Log.Warn(TAG, "Texture changed while compressing {0}", glTexture.Handle);
                        return;
                    }

                    glTexture.UploadFull(
                        source.Width,
                        source.Height,
                        source.Depth,
                        newData[0].Format,
                        newData[0].Compression,
                        newData,
                        newData[0].BlockSize);

                    Log.Debug(TAG, "Upload done {0}", glTexture.Handle);
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, ex);
                }
                finally
                {
                    source.UpdateTask = null;
                    source.NotifyLoaded();
                }
            }

            public void Update(Texture2D texture2D)
            {

#if GLES
                //Necessary for generate mips

                if ((texture2D.MipLevelCount > 1 || texture2D.MinFilter == ScaleFilter.LinearMipmapLinear) &&
                    texture2D.Data != null &&
                    texture2D.Data.Count == 1 &&
                   !texture2D.Data[0].Format.CanGenerateMipmaps(true))
                {
                    texture2D.Data[0] = ImageUtils.PackToRgba8(texture2D.Data[0], 1);
                    texture2D.Format = texture2D.Data[0].Format;
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
                    glTexture.UpdateSampler();
                    return;
                }

                if (texture2D.Data != null)
                {
                    if (texture2D.UpdateTask != null)
                        return;

                    var compInfo = texture2D.ShouldCompress();

                    var data = texture2D.Data;

                    if (compInfo != null)
                        texture2D.UpdateTask = Task.Run(() => glTexture.CompressAsync(texture2D, compInfo.Value));

                    glTexture.UploadFull(
                        texture2D.Width,
                        texture2D.Height,
                        texture2D.Depth,
                        texture2D.Format,
                        texture2D.Compression,
                        data,
                        0);

                    if (compInfo == null)
                        texture2D.NotifyLoaded();

                    return;
                }

                glTexture.Allocate(
                    texture2D.Width,
                    texture2D.Height,
                    texture2D.Depth,
                    texture2D.Format);
            }

            void ApplyDescription(Texture2D texture2D)
            {
                glTexture.SetTarget(texture2D.GetTarget());

                glTexture.EnableDebug = (texture2D.Flags & EngineObjectFlags.EnableDebug) != 0;
                glTexture.MinFilter = (TextureMinFilter)texture2D.MinFilter;
                glTexture.MagFilter = (TextureMagFilter)texture2D.MagFilter;
                glTexture.WrapS = (TextureWrapMode)texture2D.WrapS;
                glTexture.WrapT = (TextureWrapMode)texture2D.WrapT;
                glTexture.SampleCount = texture2D.SampleCount;
                glTexture.BorderColor = texture2D.BorderColor;
                glTexture.IsMutable = (texture2D.Flags & EngineObjectFlags.Mutable) != 0;
                glTexture.MaxAnisotropy = texture2D.MaxAnisotropy;
                glTexture.MaxLevel = texture2D.GetMaxLevel();

                if (string.IsNullOrWhiteSpace(glTexture.Label))
                    glTexture.SetLabel(texture2D.Name ?? "Texture");

                glTexture.Version = texture2D.Version;
                glTexture.Source = texture2D;

                texture2D.Handle = glTexture.Handle;

                if (texture2D is Texture3D tex3d)
                {
                    glTexture.WrapR = (TextureWrapMode)tex3d.WrapR;
                }
            }

            public Texture ToEngineTexture(TextureFormat? readFormat = null)
            {
                if (glTexture.Source is Texture texture)
                    return texture;

                Texture2D result;

                if (glTexture.Target == TextureTarget.TextureCubeMap)
                    result = new TextureCube();
                else if (glTexture.Target == TextureTarget.Texture3D)
                    result = new Texture3D();
                else
                    result = new Texture2D();

                glTexture.ToEngineTexture(result, readFormat);

                glTexture.Source = result;

                return result;
            }

            internal Texture2D ToEngineTexture(Texture2D result, TextureFormat? readFormat = null)
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
                result.Format = glTexture.InternalFormat.ToTextureFormat();
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

            public GlTexture Clone(bool includeContent)
            {
                var result = new GlTexture(glTexture.GL)
                {
                    Target = glTexture.Target,
                    MinFilter = glTexture.MinFilter,
                    MagFilter = glTexture.MagFilter,
                    WrapS = glTexture.WrapS,
                    WrapT = glTexture.WrapT,
                    WrapR = glTexture.WrapR,
                    MaxAnisotropy = glTexture.MaxAnisotropy,
                    BorderColor = glTexture.BorderColor,
                    MaxLevel = glTexture.MaxLevel,
                    BaseLevel = glTexture.BaseLevel,
                    IsMutable = glTexture.IsMutable,
                    SampleCount = glTexture.SampleCount
                };

                var texFormat = glTexture.InternalFormat.ToTextureFormat();

                result.Allocate(
                    glTexture.Width,
                    glTexture.Height,
                    glTexture.Depth,
                    texFormat);

                if (includeContent)
                    glTexture.CopyTo(result);

                return result;
            }

            [Conditional("DEBUG")]
            public unsafe void DumpState(string? name = null)
            {
                var gl = glTexture.GL;
                var target = glTexture.Target;

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

                GlState.Current.BindTexture(target, glTexture);

                Debug.WriteLine("");
                Debug.WriteLine($"--- Texture state {(name != null ? $"[{name}]" : "")} ---");
                Debug.WriteLine($"Handle: {glTexture}");
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

                var err = gl.GetError();

                if (err != GLEnum.NoError)
                    Debug.WriteLine($"GL error after DumpTextureState: {err}");

                Debug.WriteLine("--- End texture state ---");
                Debug.WriteLine("");
            }
        }

        #endregion

        #region OpenGLRender

        extension(OpenGLRender self)
        {
            public T? Pass<T>() where T : IGlRenderPass
            {
                return self.Passes<T>().Single();
            }

            public bool HasPass<T>() where T : IGlRenderPass
            {
                return self.Passes<T>().Any();
            }

            public void SetScissor(Rect2I rect)
            {
                self.State.EnableFeature(EnableCap.ScissorTest, true);

                self.GL.Scissor(rect.X, rect.Y, rect.Width, rect.Height);
            }

            public void SetScissor(Bounds2 bounds, int padding = 0)
            {
                self.SetScissor(bounds.ToRect2I(padding));
            }
        }

        #endregion

        #region GL

        extension(GL gl)
        {
            public ExtFragmentShadingRate ShadingRateExt
            {
                get
                {
                    if (_fsRateExt == null)
                        gl.TryGetExtension(out _fsRateExt);
                    return _fsRateExt ?? throw new NotSupportedException();
                }
            }

            public ExtShaderFramebufferFetchNonCoherent FbFetchNonCoherentExt
            {
                get
                {
                    if (_fbFetchExt == null)
                        gl.TryGetExtension(out _fbFetchExt);
                    return _fbFetchExt ?? throw new NotSupportedException();
                }
            }

            public void ClearError()
            {
                while (gl.GetError() != GLEnum.NoError) ;
            }

            public bool CheckError(bool log = true)
            {
                GLEnum err;

                var hasError = false;

                while ((err = gl.GetError()) != GLEnum.NoError)
                {
                    if (log)
                        Log.Warn("CheckError", "{0}:\n{1}", err, Environment.StackTrace);

                    hasError = true;
                }

                return hasError;
            }

            public TextureTarget FindTextureTarget(uint texId)
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

                        GlState.Current.LoadTexture(texId, target, 0);

                        gl.GetInteger(bindings[i], out var curTexId);

                        GlState.Current.BindTexture(target, 0);

                        gl.ClearError();

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

            public Texture TexIdToEngineTexture(uint texId, TextureFormat? readFormat = null)
            {
                return GlTexture.Attach(gl, texId).ToEngineTexture(readFormat);
            }

            public uint GetActiveBufferBinding(BufferTargetARB target)
            {
                var pname = target switch
                {
                    BufferTargetARB.ArrayBuffer => GetPName.ArrayBufferBinding,
                    BufferTargetARB.ElementArrayBuffer => GetPName.ElementArrayBufferBinding,
                    BufferTargetARB.UniformBuffer => GetPName.UniformBufferBinding,
                    BufferTargetARB.ShaderStorageBuffer => GetPName.ShaderStorageBufferBinding,
                    BufferTargetARB.PixelPackBuffer => GetPName.PixelPackBufferBinding,
                    BufferTargetARB.PixelUnpackBuffer => GetPName.PixelUnpackBufferBinding,
                    BufferTargetARB.TransformFeedbackBuffer => GetPName.TransformFeedbackBufferBinding,
                    BufferTargetARB.DispatchIndirectBuffer => GetPName.DispatchIndirectBufferBinding,
                    BufferTargetARB.CopyWriteBuffer => (GetPName)GLEnum.CopyWriteBufferBinding,
                    BufferTargetARB.CopyReadBuffer => (GetPName)GLEnum.CopyReadBufferBinding,
                    _ => throw new NotSupportedException($"Unsupported buffer target: {target}")
                };

                return (uint)gl.GetInteger(pname);
            }

            public uint GetActiveTextureBinding(TextureTarget target)
            {
                var binding = target switch
                {
                    TextureTarget.Texture1D => GetPName.TextureBinding1D,
                    TextureTarget.Texture2D => GetPName.TextureBinding2D,
                    TextureTarget.Texture3D => GetPName.TextureBinding3D,
                    TextureTarget.Texture1DArray => GetPName.TextureBinding1DArray,
                    TextureTarget.Texture2DArray => GetPName.TextureBinding2DArray,
                    TextureTarget.TextureRectangle => GetPName.TextureBindingRectangle,
                    TextureTarget.TextureCubeMap => GetPName.TextureBindingCubeMap,
                    TextureTarget.Texture2DMultisample => GetPName.TextureBinding2DMultisample,
                    TextureTarget.Texture2DMultisampleArray => GetPName.TextureBinding2DMultisampleArray,
                    TextureTarget.TextureBuffer => GetPName.TextureBindingBuffer,
                    (TextureTarget)0x8D65 => (GetPName)0x8D67, // GL_TEXTURE_BINDING_EXTERNAL_OES

                    _ => throw new NotSupportedException($"Unsupported texture target: {target}")
                };

                return (uint)gl.GetInteger(binding);
            }
        }

        #endregion

    }
}
