#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;


namespace XrEngine.OpenGL
{
    public class GlIBLProcessor : IDisposable
    {
        public enum Distribution
        {
            Lambertian,
            GGX,
            Charlie
        }


        private readonly GL _gl;
        private GlTexture? _inputTexture;
        private GlBuffer<Vector4>? _hams;
        private uint _cubeMapId;
        private uint _frameBufferId;
        private uint _fooVa;
        private GlSimpleProgram? _panToCubeProg;
        private GlSimpleProgram? _filterProg;


        public GlIBLProcessor(GL gl)
        {
            _gl = gl;
            EnvFormat = InternalFormat.Rgba16f;
            LutFormat = InternalFormat.Rgba16f;
        }

        public unsafe void Initialize(TextureData panoramaHdr, Func<string, string> shaderResolver)
        {
            Dispose();

            _frameBufferId = _gl.GenFramebuffer();

            _inputTexture = LoadHdr(panoramaHdr);

            _hams = new GlBuffer<Vector4>(_gl, BufferTargetARB.UniformBuffer);
            _hams.AssignSlot();
            _hams.Update(GenerateHammersley((int)SampleCount).Select(a => new Vector4(a.X, a.Y, 0, 0)).ToArray().AsSpan());

            _fooVa = _gl.GenVertexArray();

            var maxMipLevels = (uint)MathF.Floor(MathF.Log2(Resolution));

            if (MipLevelCount == 0 || MipLevelCount > maxMipLevels)
                MipLevelCount = maxMipLevels;

            _panToCubeProg = new GlSimpleProgram(_gl, 
                shaderResolver("Ibl/fullscreen.vert"), 
                shaderResolver("Ibl/panorama_to_cubemap.frag"),
                shaderResolver);


            _panToCubeProg.AddFeature("SAMPLE_COUNT 1");
            _panToCubeProg.Build();

            _filterProg = new GlSimpleProgram(_gl,
                shaderResolver("Ibl/fullscreen.vert"),
                shaderResolver("Ibl/filter_cubemap.frag"),
                shaderResolver);

            _filterProg.AddFeature("SAMPLE_COUNT " + SampleCount);
            _filterProg.Build();
            _filterProg.Use();

            _filterProg.SetUniform("uCubeMap", 0);
            _filterProg.SetUniform("uSampleCount", (float)SampleCount);
            _filterProg.SetUniform("uLodBias", (float)LodBias);
            _filterProg.SetUniform("uWidth", (float)Resolution);
            _filterProg.SetUniform("HammersleyBuffer", (IBuffer)_hams);

  
            _gl.BindVertexArray(_fooVa);
            _gl.ColorMask(true, true, true, true);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.CullFace);
            _gl.Disable(EnableCap.DepthTest);
            _gl.FrontFace(FrontFaceDirection.Ccw);
        }


        public void ApplyFilter(Distribution distribution, out uint envTexId, out uint lutTexId)
        {
            var mipCount = distribution == Distribution.Lambertian ? 1 : MipLevelCount;

            envTexId = CreateCubeMap(mipCount > 1);
            lutTexId = CreateLutTexture();

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);

            _filterProg!.Use();
            _filterProg.SetUniform("uDistribution", (int)distribution);


            for (var mipLevel = 0; mipLevel < mipCount; ++mipLevel)
            {
                ApplyFilter(
                    distribution,
                    mipLevel,
                    envTexId, lutTexId);
            }
        }

        public void PanoramaToCubeMap()
        {
            if (_cubeMapId == 0)
                _cubeMapId = CreateCubeMap(true);

            _gl.ClearColor(0, 0, 0, 1);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _inputTexture!.Bind();

            _gl.UseProgram(_panToCubeProg!.Handle);

            _panToCubeProg.SetUniform("uPanorama", 0);

            BindFrameBufferCube(_cubeMapId);

            _gl.Viewport(0, 0, Resolution, Resolution);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);
            _gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        }


        protected void BindFrameBufferCube(uint cubeTexId, int mipLevel = 0)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBufferId);

            for (var i = 0; i < 6; ++i)
            {
                _gl.FramebufferTexture2D(
                     FramebufferTarget.Framebuffer,
                     FramebufferAttachment.ColorAttachment0 + i,
                     TextureTarget.TextureCubeMapPositiveX + i,
                     cubeTexId, mipLevel);
            }

            List<DrawBufferMode> targets = [DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1, DrawBufferMode.ColorAttachment2, DrawBufferMode.ColorAttachment3, DrawBufferMode.ColorAttachment4, DrawBufferMode.ColorAttachment5];
            if (mipLevel == 0)
                targets.Add(DrawBufferMode.ColorAttachment6);


            var buffers = new ReadOnlySpan<DrawBufferMode>(targets.ToArray());
            _gl.DrawBuffers(buffers);
        }


        protected void ApplyFilter(Distribution distribution, int mipLevel, uint envTexId, uint lutTexId)
        {
            var currentTextureSize = Resolution >> mipLevel;

            var roughness = (mipLevel) / (MipLevelCount - 1f);

            _filterProg!.SetUniform("uRoughness", (float)roughness);
            _filterProg.SetUniform("uCurrentMipLevel", (int)mipLevel);


            BindFrameBufferCube(envTexId, mipLevel);

            if (lutTexId != 0)
            {
                _gl.FramebufferTexture2D(
                     FramebufferTarget.Framebuffer,
                     FramebufferAttachment.ColorAttachment6,
                     TextureTarget.Texture2D,
                     lutTexId, 0);
            }

            _gl.Viewport(0, 0, currentTextureSize, currentTextureSize);

            _gl.ClearColor(0, 0, 0, 1);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        public unsafe Vector2[] GenerateHammersley(int sampleCount)
        {

            float radicalInverse_VdC(uint bits)
            {
                bits = (bits << 16) | (bits >> 16);
                bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
                bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
                bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
                bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);
                return ((float)bits) * 2.3283064365386963e-10f; // / 0x100000000
            }


            var buf = new Vector2[sampleCount];
            for (var i = 0; i < sampleCount; i++)
                buf[i] = new Vector2(i / (float)sampleCount, radicalInverse_VdC((uint)i));

            return buf;
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
            else if (data.Format == TextureFormat.Rgb24)
                format = TextureFormat.Rgba32;
            else
                format = data.Format;

            res.Update(data.Width, data.Height, format, data.Compression, [data]);

            return res;
        }


        protected unsafe uint CreateLutTexture()
        {
            var targetTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, targetTexture);

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

        protected unsafe uint CreateCubeMap(bool withMipmaps)
        {
            var targetTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.TextureCubeMap, targetTexture);


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
            {
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, (int)MipLevelCount - 1);
                //_gl.GenerateMipmap(TextureTarget.TextureCubeMap);   
            }
            else
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, 0);

            return targetTexture;
        }


        public void Dispose()
        {
            _inputTexture?.Dispose();
            _hams?.Dispose();

            if (_fooVa != 0)
                _gl.DeleteVertexArray(_fooVa);

            //TODO uncomment
            /*
            if (_cubeMapId != 0)
                _gl.DeleteTexture(_cubeMapId);
            */

            if (_frameBufferId != 0)
                _gl.DeleteFramebuffer(_frameBufferId);

            _panToCubeProg?.Dispose();
            _filterProg?.Dispose();

            _panToCubeProg = null;
            _filterProg = null;
            _hams = null;
            _inputTexture = null;
            _cubeMapId = 0;
            _fooVa = 0;

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.UseProgram(0);
            _gl.BindTexture(TextureTarget.Texture2D, 0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public uint OutCubeMapId => _cubeMapId;

        public InternalFormat EnvFormat { get; set; }

        public InternalFormat LutFormat { get; set; }

        public uint Resolution { get; set; }

        public uint SampleCount { get; set; }

        public float LodBias { get; set; }

        public uint MipLevelCount { get; set; }
    }
}
