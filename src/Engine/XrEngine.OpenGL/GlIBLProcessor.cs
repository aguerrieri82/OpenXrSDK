#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlIBLProcessor : IDisposable
    {
        protected enum Distribution
        {
            Lambertian,
            GGX,
            Charlie
        }


        private readonly GL _gl;
        private uint _inputTextureId;
        private uint _cubeMapId;
        private uint _frameBufferId;
        private uint _lambertianTextureId;
        private uint _ggxTextureId;
        private uint _sheenTextureId;
        private uint _fooVa;
        private uint _ggxLutTextureId;
        private uint _charlieLutTextureId;
        private uint _mipmapLevels;
        private GlSimpleProgram? _panToCubeProg;
        private GlSimpleProgram? _filterProg;
        private bool _keepGenTextures;

        public GlIBLProcessor(GL gl)
        {
            _gl = gl;
            _keepGenTextures = true;
        }

        public void Initialize(TextureData panoramaHdr, Func<string, string> shaderResolver)
        {
            Dispose();

            _frameBufferId = _gl.GenFramebuffer();

            _inputTextureId = LoadHdr(panoramaHdr);
            _cubeMapId = CreateCubeMap(true);
            _fooVa = _gl.GenVertexArray();

            _mipmapLevels = (uint)MathF.Floor(MathF.Log2(TextureSize)) + 1 - LowestMipLevel;

            _panToCubeProg = new GlSimpleProgram(_gl, 
                shaderResolver("Ibl/fullscreen.vert"), 
                shaderResolver("Ibl/panorama_to_cubemap.frag"),
                shaderResolver);

            _panToCubeProg.Build();

            _filterProg = new GlSimpleProgram(_gl,
                shaderResolver("Ibl/fullscreen.vert"),
                shaderResolver("Ibl/ibl_filtering.frag"),
                shaderResolver);

            _filterProg.Build();

            _gl.BindVertexArray(_fooVa);
            _gl.ColorMask(true, true, true, true);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.CullFace);
            _gl.Disable(EnableCap.DepthTest);
            _gl.FrontFace(FrontFaceDirection.Ccw);
        }

        public void ProcessAll()
        {
            PanoramaToCubeMap();
            
            CubeMapToLambertian();
            
            CubeMapToGGX();
            SampleGGXLut();

            CubeMapToSheen();

            SampleCharlieLut();
        }

        public void SampleGGXLut()
        {
            if (_ggxLutTextureId == 0)
                _ggxLutTextureId = CreateLutTexture();

            SampleLut(Distribution.GGX, _ggxLutTextureId, LutResolution);
        }

        public void SampleCharlieLut()
        {
            if (_charlieLutTextureId == 0)
                _charlieLutTextureId = CreateLutTexture();
            SampleLut(Distribution.Charlie, _charlieLutTextureId, LutResolution);
        }


        public void CubeMapToLambertian()
        {
            if (_lambertianTextureId == 0)
                _lambertianTextureId = CreateCubeMap(false);

            ApplyFilter(Distribution.Lambertian,
                0,
                0,
                _lambertianTextureId,
                LambertianSampleCount,
                LodBias);
        }

        public void CubeMapToGGX()
        {
            if (_ggxTextureId == 0)
                _ggxTextureId = CreateCubeMap(true);

            for (var currentMipLevel = 0; currentMipLevel <= _mipmapLevels; ++currentMipLevel)
            {
                var roughness = (currentMipLevel) / (_mipmapLevels - 1f);
                ApplyFilter(
                    Distribution.GGX,
                    roughness,
                    currentMipLevel,
                    _ggxTextureId,
                    GGXSampleCount,
                    LodBias);
            }
        }

        public void CubeMapToSheen()
        {
            if (_sheenTextureId == 0)
                _sheenTextureId = CreateCubeMap(true);

            for (var currentMipLevel = 0; currentMipLevel <= _mipmapLevels; ++currentMipLevel)
            {
                var roughness = (currentMipLevel) / (_mipmapLevels - 1f);
                ApplyFilter(
                    Distribution.Charlie,
                    roughness,
                    currentMipLevel,
                    _sheenTextureId,
                    SheenSampleCount,
                    LodBias);
            }
        }

        protected void SampleLut(Distribution distribution, uint targetTexture, uint currentTextureSize)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBufferId);

            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                targetTexture, 0);


            _gl.Viewport(0, 0, currentTextureSize, currentTextureSize);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);

            _gl.UseProgram(_filterProg!.Handle);
 
            _filterProg.SetUniform("uCubeMap", 0);
            _filterProg.SetUniform("u_roughness", 0);
            _filterProg.SetUniform("u_sampleCount", 512);
            _filterProg.SetUniform("u_width", 0);
            _filterProg.SetUniform("u_lodBias", 0f);
            _filterProg.SetUniform("u_distribution", (int)distribution);
            _filterProg.SetUniform("u_currentFace", 0);
            _filterProg.SetUniform("u_isGeneratingLUT", 1);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }


        protected void ApplyFilter(Distribution distribution, float roughness, int targetMipLevel, uint targetTexture, uint sampleCount, float lodBias = 0)
        {
            var currentTextureSize = TextureSize >> targetMipLevel;

            _gl.Viewport(0, 0, currentTextureSize, currentTextureSize);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);

            _filterProg!.Use();

            _filterProg.SetUniform("uCubeMap", 0);
            _filterProg.SetUniform("u_roughness", roughness);
            _filterProg.SetUniform("u_sampleCount", (int)sampleCount);
            _filterProg.SetUniform("u_width", (int)TextureSize);
            _filterProg.SetUniform("u_lodBias", lodBias);
            _filterProg.SetUniform("u_distribution", (int)distribution);
            _filterProg.SetUniform("u_isGeneratingLUT", (int)0);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBufferId);

            for (var i = 0; i < 6; ++i)
            {
                _gl.FramebufferTexture2D(
                     FramebufferTarget.Framebuffer,
                     FramebufferAttachment.ColorAttachment0,
                     TextureTarget.TextureCubeMapPositiveX + i,
                     targetTexture, targetMipLevel);

                _filterProg.SetUniform("u_currentFace", i);

                _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
        }
    

        public void PanoramaToCubeMap()
        {

            _gl.Viewport(0, 0, TextureSize, TextureSize);
            _gl.ClearColor(0, 0, 0, 1);
 
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _inputTextureId);

            _gl.UseProgram(_panToCubeProg!.Handle);

            _panToCubeProg.SetUniform("u_panorama", 0);

            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _frameBufferId);


            for (var i = 0; i < 6; ++i)
            {
                _gl.FramebufferTexture2D(
                     FramebufferTarget.DrawFramebuffer,
                     FramebufferAttachment.ColorAttachment0,
                     TextureTarget.TextureCubeMapPositiveX + i,
                     _cubeMapId, 0);

                _panToCubeProg.SetUniform("u_currentFace", i);

                _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }

            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);
            _gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        }

        protected unsafe uint LoadHdr(TextureData data)
        {
            if (data.Format != TextureFormat.RgbFloat)
                throw new NotSupportedException();

            var targetTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, targetTexture);

            fixed (byte* pData = data.Data)
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgb32f,
                    data.Width,
                    data.Height,
                    0,
                    PixelFormat.Rgb,
                    PixelType.Float,
                    pData
                );

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.MirroredRepeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.MirroredRepeat);

            return targetTexture;
        }


        protected unsafe uint CreateLutTexture()
        {
            var targetTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, targetTexture);

            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                Use8Bit ? InternalFormat.Rgba8 : InternalFormat.Rgba32f,
                LutResolution,
                LutResolution,
                0,
                PixelFormat.Rgba,
                Use8Bit ? PixelType.UnsignedByte : PixelType.Float,
                null
            ); 
            
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            return targetTexture;
        }

        protected unsafe uint CreateCubeMap(bool withMipmaps)
        {
            var targetTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.TextureCubeMap, targetTexture);

            for (var i = 0; i < 6; ++i)
            {
                _gl.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + i,
                    0,
                    Use8Bit ? InternalFormat.Rgba8 : InternalFormat.Rgba32f,
                    TextureSize,
                    TextureSize,
                    0,
                    PixelFormat.Rgba,
                    Use8Bit ? PixelType.UnsignedByte : PixelType.Float,
                    null
                );
            }

            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)(withMipmaps ? GLEnum.LinearMipmapLinear : GLEnum.Linear));
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            if (withMipmaps)
            {
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, (int)_mipmapLevels);
                _gl.GenerateMipmap(TextureTarget.TextureCubeMap);   
            }
        

            return targetTexture;
        }


        public void Dispose()
        {
            if (_inputTextureId != 0)
                _gl.DeleteTexture(_inputTextureId);

            if (_fooVa != 0)
                _gl.DeleteVertexArray(_fooVa);

            if (!_keepGenTextures)
            {
                if (_cubeMapId != 0)
                    _gl.DeleteTexture(_cubeMapId);

                if (_lambertianTextureId != 0)
                    _gl.DeleteTexture(_lambertianTextureId);

                if (_ggxTextureId != 0)
                    _gl.DeleteTexture(_ggxTextureId);

                if (_sheenTextureId != 0)
                    _gl.DeleteTexture(_sheenTextureId);

                if (_charlieLutTextureId != 0)
                    _gl.DeleteTexture(_charlieLutTextureId);

                if (_ggxLutTextureId != 0)
                    _gl.DeleteTexture(_ggxLutTextureId);

                _lambertianTextureId = 0;
                _ggxTextureId = 0;
                _sheenTextureId = 0;
                _charlieLutTextureId = 0;
                _ggxLutTextureId = 0;
            }


            if (_frameBufferId != 0)
                _gl.DeleteFramebuffer(_frameBufferId);

            if (_panToCubeProg != null)
                _panToCubeProg.Dispose();

            if (_filterProg != null)
                _filterProg.Dispose();

            _panToCubeProg = null;
            _filterProg = null;
            _inputTextureId = 0;
            _cubeMapId = 0;
            _fooVa = 0;

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.UseProgram(0);
            _gl.BindTexture(TextureTarget.Texture2D, 0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public uint OutMipMapLevels => _mipmapLevels;

        public uint OutLambertianTextureId => _lambertianTextureId;

        public uint OutCubeMapId => _cubeMapId;

        public uint OutGGXTextureId => _ggxTextureId;

        public uint OutSheenTextureId => _sheenTextureId;

        public uint OutGGXLutTextureId => _ggxLutTextureId;

        public uint OutCharlieLutTextureId => _charlieLutTextureId;


        public bool Use8Bit;

        public uint TextureSize;
        
        public uint GGXSampleCount;
        
        public uint LambertianSampleCount;
        
        public uint SheenSampleCount;
        
        public float LodBias;
        
        public uint LowestMipLevel;

        public uint LutResolution;
    }
}
