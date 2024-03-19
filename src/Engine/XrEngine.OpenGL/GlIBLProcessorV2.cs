#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Collections.Generic;
#endif


namespace XrEngine.OpenGL
{
    public class GlIBLProcessorV2 : IDisposable
    {
        public enum Distribution
        {
            Lambertian,
            GGX,
            Charlie
        }


        private readonly GL _gl;
        private uint _inputTextureId;
        private uint _cubeMapId;
        private uint _frameBufferId;
        private uint _fooVa;
        private GlSimpleProgram? _panToCubeProg;
        private GlSimpleProgram? _filterProg;


        public GlIBLProcessorV2(GL gl)
        {
            _gl = gl;

        }

        public void Initialize(TextureData panoramaHdr, Func<string, string> shaderResolver)
        {
            Dispose();

            _frameBufferId = _gl.GenFramebuffer();

            _inputTextureId = LoadHdr(panoramaHdr);
            _cubeMapId = CreateCubeMap(true);
            _fooVa = _gl.GenVertexArray();

            if (MipCount == 0)
                MipCount = (uint)MathF.Floor(MathF.Log2(Resolution));

            _panToCubeProg = new GlSimpleProgram(_gl, 
                shaderResolver("Ibl/fullscreen.vert"), 
                shaderResolver("Ibl/panorama_to_cubemap_v2.frag"),
                shaderResolver);

            _panToCubeProg.Build();

            _filterProg = new GlSimpleProgram(_gl,
                shaderResolver("Ibl/fullscreen.vert"),
                shaderResolver("Ibl/filter_cubemap.frag"),
                shaderResolver);

            _filterProg.Build();

            _gl.BindVertexArray(_fooVa);
            _gl.ColorMask(true, true, true, true);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.CullFace);
            _gl.Disable(EnableCap.DepthTest);
            _gl.FrontFace(FrontFaceDirection.Ccw);
        }


        public void ApplyFilter(Distribution distribution, out uint envTexId, out uint lutTexId)
        {
            var mipCount = distribution == Distribution.Lambertian ? 1 : MipCount;

            envTexId = CreateCubeMap(mipCount > 1);
            lutTexId = CreateLutTexture();

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
            _gl.ClearColor(0, 0, 0, 1);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _inputTextureId);

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


        protected void ApplyFilter(Distribution distribution, int mipLevel, uint cubTexId, uint lutTexId)
        {
            var currentTextureSize = Resolution >> mipLevel;

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, _cubeMapId);

            _filterProg!.Use();

            var roughness = (mipLevel) / (MipCount - 1f);

            _filterProg.SetUniform("uCubeMap", 0);
            _filterProg.SetUniform("pFilterParameters.roughness", roughness);
            _filterProg.SetUniform("pFilterParameters.sampleCount", (int)SampleCount);
            _filterProg.SetUniform("pFilterParameters.width", (int)Resolution);
            _filterProg.SetUniform("pFilterParameters.lodBias", LodBias);
            _filterProg.SetUniform("pFilterParameters.distribution", (int)distribution);
            _filterProg.SetUniform("pFilterParameters.currentMipLevel", (int)mipLevel);

            BindFrameBufferCube(cubTexId, mipLevel);

            if (lutTexId != 0)
            {
                _gl.FramebufferTexture2D(
                     FramebufferTarget.Framebuffer,
                     FramebufferAttachment.ColorAttachment6,
                     TextureTarget.Texture2D,
                     lutTexId, 0);
            }

            _gl.Viewport(0, 0, currentTextureSize, currentTextureSize);

            _gl.ClearColor(0, 0, 0, 0);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
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
                Use8Bit ? InternalFormat.Rgb8 : InternalFormat.Rgb32f,
                Resolution,
                Resolution,
                0,
                PixelFormat.Rgb,
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
                    Resolution,
                    Resolution,
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
                _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, (int)MipCount);
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

            if (_cubeMapId != 0)
                _gl.DeleteTexture(_cubeMapId);

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


        public uint OutCubeMapId => _cubeMapId;

        public bool Use8Bit;

        public uint Resolution;

        public uint SampleCount;

        public float LodBias;

        public uint MipCount;
    }
}
