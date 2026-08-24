#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlIblProcessor : IDisposable
    {
        public enum Distribution
        {
            Irradiance,
            GGX,
            GGXLut,
            Charlie,
            CharlieLut,
            SheenLut
        }

        private readonly GL _gl;
        private GlTexture? _inputTexture;
        private uint _cubeMapId;
        private uint _frameBufferId;
        private GlComputeProgram? _panToCubeProg;
        private readonly Dictionary<Distribution, GlComputeProgram> _filterProg;

        public GlIblProcessor(GL gl)
        {
            _gl = gl;
            _filterProg = [];
            EnvFormat = InternalFormat.Rgba16f;
            LutFormat = InternalFormat.Rgba16f;
            Resolution = 512;
        }

        public void Initialize(TextureData panoramaHdr, Func<string, string> shaderResolver)
        {
            Dispose();

            _frameBufferId = _gl.GenFramebuffer();

            _inputTexture = LoadHdr(panoramaHdr);

            var maxMipLevels = (uint)MathF.Floor(MathF.Log2(Resolution));

            if (MipLevelCount == 0 || MipLevelCount > maxMipLevels)
                MipLevelCount = maxMipLevels;

            _panToCubeProg = new GlComputeProgram(_gl,
                "Ibl/equirect2cube_cs.comp",
                shaderResolver);

            _panToCubeProg.Build();

            void AddFilter(string shader, Distribution distribution)
            {
                var prog = new GlComputeProgram(_gl, shader, shaderResolver);
                prog.AddFeature($"SAMPLE_COUNT {SampleCount * (distribution == Distribution.Irradiance ? 4 : 1)}u");
                prog.Build();
                _filterProg[distribution] = prog;
            }

            AddFilter("Ibl/irmap_cs.comp", Distribution.Irradiance);
            AddFilter("Ibl/spbrdf_cs.comp", Distribution.GGXLut);
            AddFilter("Ibl/spmap_cs.comp", Distribution.GGX);
            AddFilter("Ibl/chbrdf_cs.comp", Distribution.CharlieLut);
            AddFilter("Ibl/chmap_cs.comp", Distribution.Charlie);

        }

        public void PanoramaToCubeMap()
        {
            if (_cubeMapId == 0)
                _cubeMapId = CreateCubeMap(true);

            _panToCubeProg!.Use();

            GlState.Current.LoadTexture(_inputTexture!, 0);

            _panToCubeProg.SetUniform("inputTexture", 0);

            var steps = (Resolution + 15) / 16;

            _gl.BindImageTexture(0, _cubeMapId, 0, true, 0, BufferAccessARB.WriteOnly, EnvFormat);

            _gl.DispatchCompute(steps, steps, 6);

            _gl.MemoryBarrier(MemoryBarrierMask.TextureUpdateBarrierBit);

            GlState.Current.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);

            _gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        }
        public uint ApplyFilter(Distribution distribution)
        {
            var program = _filterProg![distribution];
            var isLut = distribution == Distribution.GGXLut || distribution == Distribution.CharlieLut || distribution == Distribution.SheenLut;
            var isMip = distribution == Distribution.GGX || distribution == Distribution.Charlie;
            var mipCount = isMip ? MipLevelCount : 1;

            uint texId;
            if (isLut)
                texId = CreateLutTexture();
            else
                texId = CreateCubeMap(mipCount > 1);

            program!.Use();

            if (!isLut)
            {
                program.SetUniform("inputTexture", 0);
                GlState.Current.LoadTexture(_cubeMapId, TextureTarget.TextureCubeMap, 0);
            }

            var res = Resolution;
            for (var mipLevel = 0; mipLevel < mipCount; ++mipLevel)
            {
                if (isMip)
                {
                    if (mipLevel == 0)
                    {
                        _gl.CopyImageSubData(_cubeMapId, CopyImageSubDataTarget.TextureCubeMap, 0, 0, 0, 0,
                                             texId, CopyImageSubDataTarget.TextureCubeMap, 0, 0, 0, 0,
                                             Resolution, Resolution, 6);

                        res = res >> 1;

                        continue;
                    }
                    else
                    {
                        var r = mipLevel / ((float)mipCount - 1);
                        program.SetUniform("roughness", r);
                    }
                }

                var steps = (res + 15) / 16;

                _gl.BindImageTexture(0, texId, mipLevel, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

                _gl.DispatchCompute(steps, steps, isLut ? 1u : 6u);

                res = res >> 1;
            }

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);

            return texId;
        }

        protected GlTexture LoadHdr(TextureData data)
        {
            var res = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMagFilter.Linear,
                WrapT = TextureWrapMode.MirroredRepeat,
                WrapS = TextureWrapMode.MirroredRepeat,
                MaxLevel = 0
            };

            TextureFormat format;

            if (data.Format == TextureFormat.RgbFloat16)
                format = TextureFormat.RgbaFloat16;
            else if (data.Format == TextureFormat.Rgb8)
                format = TextureFormat.Rgba8;
            else
                format = data.Format;

            res.UpdateFull(data);

            return res;
        }

        protected uint CreateLutTexture()
        {
            var targetTexture = _gl.GenTexture();
            GlState.Current.BindTexture(TextureTarget.Texture2D, targetTexture);

            _gl.TexStorage2D(
                TextureTarget.Texture2D,
                1,
                (SizedInternalFormat)LutFormat,
                Resolution,
                Resolution
            );

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

            return targetTexture;
        }

        protected uint CreateCubeMap(bool withMipmaps)
        {
            var targetTexture = _gl.GenTexture();
            GlState.Current.BindTexture(TextureTarget.TextureCubeMap, targetTexture);

            _gl.TexStorage2D(
                   TextureTarget.TextureCubeMap,
                   withMipmaps ? MipLevelCount : 1,
                   (SizedInternalFormat)EnvFormat,
                   Resolution,
                   Resolution
               );

            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)(withMipmaps ? GLEnum.LinearMipmapLinear : GLEnum.Linear));
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            if (withMipmaps)
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, (int)MipLevelCount - 1);
            else
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, 0);

            return targetTexture;
        }

        public void Dispose()
        {
            if (_frameBufferId != 0)
                _gl.DeleteFramebuffer(_frameBufferId);

            _panToCubeProg?.Dispose();

            foreach (var prog in _filterProg.Values)
                prog.Dispose();
            _filterProg.Clear();

            _panToCubeProg = null;
            _inputTexture = null;

            GlState.Current.BindTexture(TextureTarget.Texture2D, 0);
            GlState.Current.BindTexture(TextureTarget.TextureCubeMap, 0);

            GlState.Current.SetActiveProgram(0);

            GC.SuppressFinalize(this);
        }

        public uint OutCubeMapId => _cubeMapId;

        public InternalFormat EnvFormat { get; set; }

        public InternalFormat LutFormat { get; set; }

        public uint Resolution { get; set; }

        public uint MipLevelCount { get; set; }

        public uint SampleCount { get; set; }
    }
}
