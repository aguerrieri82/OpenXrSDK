using Common.Interop;
using System.Runtime.InteropServices;

namespace XrEngine.Compression
{
    public static class AstcCompressor
    {
        #region NATIVE 

        /** @brief The fastest, lowest quality, search preset. */
        const float ASTCENC_PRE_FASTEST = 0.0f;

        /** @brief The fast search preset. */
        const float ASTCENC_PRE_FAST = 10.0f;

        /** @brief The medium quality search preset. */
        const float ASTCENC_PRE_MEDIUM = 60.0f;

        /** @brief The thorough quality search preset. */
        const float ASTCENC_PRE_THOROUGH = 98.0f;

        /** @brief The thorough quality search preset. */
        const float ASTCENC_PRE_VERYTHOROUGH = 99.0f;

        /** @brief The exhaustive, highest quality, search preset. */
        const float ASTCENC_PRE_EXHAUSTIVE = 100.0f;


        /**
         * @brief Enable normal map compression.
         *
         * Input data will be treated a two component normal map, storing X and Y, and the codec will
         * optimize for angular error rather than simple linear PSNR. In this mode the input swizzle should
         * be e.g. rrrg (the default ordering for ASTC normals on the command line) or gggr (the ordering
         * used by BC5n).
         */
        const uint ASTCENC_FLG_MAP_NORMAL = 1 << 0;

        /**
         * @brief Enable compression heuristics that assume use of decode_unorm8 decode mode.
         *
         * The decode_unorm8 decode mode rounds differently to the decode_fp16 decode mode, so enabling this
         * flag during compression will allow the compressor to use the correct rounding when selecting
         * encodings. This will improve the compressed image quality if your application is using the
         * decode_unorm8 decode mode, but will reduce image quality if using decode_fp16.
         *
         * Note that LDR_SRGB images will always use decode_unorm8 for the RGB channels, irrespective of
         * this setting.
         */
        const uint ASTCENC_FLG_USE_DECODE_UNORM8 = 1 << 1;

        /**
         * @brief Enable alpha weighting.
         *
         * The input alpha value is used for transparency, so errors in the RGB components are weighted by
         * the transparency level. This allows the codec to more accurately encode the alpha value in areas
         * where the color value is less significant.
         */
        const uint ASTCENC_FLG_USE_ALPHA_WEIGHT = 1 << 2;

        /**
         * @brief Enable perceptual error metrics.
         *
         * This mode enables perceptual compression mode, which will optimize for perceptual error rather
         * than best PSNR. Only some input modes support perceptual error metrics.
         */
        const uint ASTCENC_FLG_USE_PERCEPTUAL = 1 << 3;

        /**
         * @brief Create a decompression-only context.
         *
         * This mode disables support for compression. This enables context allocation to skip some
         * transient buffer allocation, resulting in lower memory usage.
         */
        const uint ASTCENC_FLG_DECOMPRESS_ONLY = 1 << 4;

        /**
         * @brief Create a self-decompression context.
         *
         * This mode configures the compressor so that it is only guaranteed to be able to decompress images
         * that were actually created using the current context. This is the common case for compression use
         * cases, and setting this flag enables additional optimizations, but does mean that the context
         * cannot reliably decompress arbitrary ASTC images.
         */
        const uint ASTCENC_FLG_SELF_DECOMPRESS_ONLY = 1 << 5;

        /**
         * @brief Enable RGBM map compression.
         *
         * Input data will be treated as HDR data that has been stored in an LDR RGBM-encoded wrapper
         * format. Data must be preprocessed by the user to be in LDR RGBM format before calling the
         * compression function, this flag is only used to control the use of RGBM-specific heuristics and
         * error metrics.
         *
         * IMPORTANT: The ASTC format is prone to bad failure modes with unconstrained RGBM data; very small
         * M values can round to zero due to quantization and result in black or white pixels. It is highly
         * recommended that the minimum value of M used in the encoding is kept above a lower threshold (try
         * 16 or 32). Applying this threshold reduces the number of very dark colors that can be
         * represented, but is still higher precision than 8-bit LDR.
         *
         * When this flag is set the value of @c rgbm_m_scale in the context must be set to the RGBM scale
         * factor used during reconstruction. This defaults to 5 when in RGBM mode.
         *
         * It is recommended that the value of @c cw_a_weight is set to twice the value of the multiplier
         * scale, ensuring that the M value is accurately encoded. This defaults to 10 when in RGBM mode,
         * matching the default scale factor.
         */
        const uint ASTCENC_FLG_MAP_RGBM = 1 << 6;


        enum astcenc_profile
        {
            /** @brief The LDR sRGB color profile. */
            ASTCENC_PRF_LDR_SRGB = 0,
            /** @brief The LDR linear color profile. */
            ASTCENC_PRF_LDR,
            /** @brief The HDR RGB with LDR alpha color profile. */
            ASTCENC_PRF_HDR_RGB_LDR_A,
            /** @brief The HDR RGBA color profile. */
            ASTCENC_PRF_HDR
        };

        enum astcenc_swz
        {
            /** @brief Select the red component. */
            ASTCENC_SWZ_R = 0,
            /** @brief Select the green component. */
            ASTCENC_SWZ_G = 1,
            /** @brief Select the blue component. */
            ASTCENC_SWZ_B = 2,
            /** @brief Select the alpha component. */
            ASTCENC_SWZ_A = 3,
            /** @brief Use a constant zero component. */
            ASTCENC_SWZ_0 = 4,
            /** @brief Use a constant one component. */
            ASTCENC_SWZ_1 = 5,
            /** @brief Use a reconstructed normal vector Z component. */
            ASTCENC_SWZ_Z = 6
        };


        struct astcenc_swizzle
        {
            /** @brief The red component selector. */
            public astcenc_swz r;
            /** @brief The green component selector. */
            public astcenc_swz g;
            /** @brief The blue component selector. */
            public astcenc_swz b;
            /** @brief The alpha component selector. */
            public astcenc_swz a;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct astcenc_params
        {
            public astcenc_profile profile;
            public uint block_x;
            public uint block_y;
            public uint block_z;
            public float quality;
            public uint flags;
            public byte thread_count;
            public astcenc_swizzle swizzle;
        }

        enum astcenc_type
        {
            /** @brief Unorm 8-bit data per component. */
            ASTCENC_TYPE_U8 = 0,
            /** @brief 16-bit float per component. */
            ASTCENC_TYPE_F16 = 1,
            /** @brief 32-bit float per component. */
            ASTCENC_TYPE_F32 = 2
        };


        [DllImport("astcencoder-native")]
        static extern int Encode(nint data, int width, int height, astcenc_type dataType, ref astcenc_params parameters, nint dst, ref int dstSize);

        #endregion


        public static TextureData Encode(TextureData data, bool isNormalMap, float quality, uint blockSize)
        {
            uint flags = 0;
            if (isNormalMap)
                flags |= ASTCENC_FLG_MAP_NORMAL;

            astcenc_profile profile;
            astcenc_type type;

            if (data.Format.IsInt8())
            {
                if (data.Format.IsSrgb())
                    profile = astcenc_profile.ASTCENC_PRF_LDR_SRGB;
                else
                    profile = astcenc_profile.ASTCENC_PRF_LDR;
                type = astcenc_type.ASTCENC_TYPE_U8;
            }
            else if (data.Format.IsFloat16())
            {
                profile = astcenc_profile.ASTCENC_PRF_HDR;
                type = astcenc_type.ASTCENC_TYPE_F16;
            }
            else if (data.Format.IsFloat32())
            {
                profile = astcenc_profile.ASTCENC_PRF_HDR;
                type = astcenc_type.ASTCENC_TYPE_F32;
            }
            else
                throw new NotSupportedException();

            astcenc_swizzle swizzle;

            if (data.Format == TextureFormat.Rgb24 ||
                data.Format == TextureFormat.SRgb24 ||
                data.Format == TextureFormat.RgbFloat16 ||
                data.Format == TextureFormat.RgbFloat32)
            {
                swizzle.r = astcenc_swz.ASTCENC_SWZ_R;
                swizzle.g = astcenc_swz.ASTCENC_SWZ_G;
                swizzle.b = astcenc_swz.ASTCENC_SWZ_B;
                swizzle.a = astcenc_swz.ASTCENC_SWZ_0;
            }
            else if (data.Format == TextureFormat.Bgra32 || data.Format == TextureFormat.SBgra32)
            {
                swizzle.r = astcenc_swz.ASTCENC_SWZ_B;
                swizzle.g = astcenc_swz.ASTCENC_SWZ_G;
                swizzle.b = astcenc_swz.ASTCENC_SWZ_R;
                swizzle.a = astcenc_swz.ASTCENC_SWZ_A;
            }

            else if (data.Format == TextureFormat.Rgba32 ||
                data.Format == TextureFormat.SRgba32 ||
                data.Format == TextureFormat.RgbaFloat32 ||
                data.Format == TextureFormat.RgbaFloat16)
            {
                swizzle.r = astcenc_swz.ASTCENC_SWZ_R;
                swizzle.g = astcenc_swz.ASTCENC_SWZ_G;
                swizzle.b = astcenc_swz.ASTCENC_SWZ_B;
                swizzle.a = astcenc_swz.ASTCENC_SWZ_A;
            }
            else
                throw new NotSupportedException();

            var pars = new astcenc_params
            {
                block_x = blockSize,
                block_y = blockSize,
                block_z = 1,
                flags = flags,
                profile = profile,
                quality = quality,
                swizzle = swizzle,
                thread_count = 8
            };


            using var srcPtr = data.Data!.MemoryLock();

            var dstSize = 0;

            var result = Encode(srcPtr, (int)data.Width, (int)data.Height, type, ref pars, IntPtr.Zero, ref dstSize);
            if (result != 0)
                throw new InvalidOperationException();

            var newData = data.Clone();
            newData.Data = MemoryBuffer.Create<byte>((uint)dstSize);
            newData.Compression = TextureCompressionFormat.Astc;
            newData.BlockSize = blockSize;

            using var dstPtr = newData.Data!.MemoryLock();

            result = Encode(srcPtr, (int)data.Width, (int)data.Height, type, ref pars, dstPtr, ref dstSize);
            if (result != 0)
                throw new InvalidOperationException();

            if (newData.Format == TextureFormat.Bgra32)
                newData.Format = TextureFormat.Rgba32;

            if (newData.Format == TextureFormat.SBgra32)
                newData.Format = TextureFormat.SRgba32;

            return newData;
        }

    }
}
