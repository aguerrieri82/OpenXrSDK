using XrMath;

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlSampler : GlObject, IGlSampler
    {
        const SamplerParameterI TextureSrgbDecodeExt = (SamplerParameterI)0x8A48;
        const int DecodeSrgbExt = 0x8A49;
        const int SkipDecodeSrgbExt = 0x8A4A;

        static bool? _supportsSrgbDecode { get; set; }

        public GlSampler(GL gl)
            : base(gl)
        {
            _supportsSrgbDecode ??= gl.IsExtensionPresent("GL_EXT_texture_sRGB_decode");

            CompareFunc = DepthFunction.Lequal;

            MinFilter = TextureMinFilter.Linear;
            MagFilter = TextureMagFilter.Linear;

            WrapS = TextureWrapMode.Repeat;
            WrapT = TextureWrapMode.Repeat;
            WrapR = TextureWrapMode.Repeat;

            BorderColor = new Color(0, 0, 0, 0);

            MinLod = -1000f;
            MaxLod = 1000f;
            LodBias = 0;

            MaxAnisotropy = 0;

            DecodeSrgb = true;

            Create();
            Update();
        }

        protected void Create()
        {
            _handle = _gl.GenSampler();
        }

        public void Update()
        {
            Bind();

            _gl.SamplerParameter(_handle, SamplerParameterI.MinFilter, (int)MinFilter);
            _gl.SamplerParameter(_handle, SamplerParameterI.MagFilter, (int)MagFilter);

            _gl.SamplerParameter(_handle, SamplerParameterI.CompareFunc, (int)CompareFunc);
            _gl.SamplerParameter(_handle, SamplerParameterI.CompareMode, (int)CompareMode);

            _gl.SamplerParameter(_handle, SamplerParameterI.WrapS, (int)WrapS);
            _gl.SamplerParameter(_handle, SamplerParameterI.WrapT, (int)WrapT);
            _gl.SamplerParameter(_handle, SamplerParameterI.WrapR, (int)WrapR);

            _gl.SamplerParameter(_handle, SamplerParameterF.BorderColor, BorderColor.ToArray());

            _gl.SamplerParameter(_handle, SamplerParameterF.MinLod, MinLod);
            _gl.SamplerParameter(_handle, SamplerParameterF.MaxLod, MaxLod);
            _gl.SamplerParameter(_handle, SamplerParameterF.LodBias, LodBias);

            if (MaxAnisotropy > 0)
                _gl.SamplerParameter(_handle, SamplerParameterF.MaxAnisotropy, MaxAnisotropy);

            if (_supportsSrgbDecode == true)
            {
                _gl.SamplerParameter(
                    _handle,
                    TextureSrgbDecodeExt,
                    DecodeSrgb ? DecodeSrgbExt : SkipDecodeSrgbExt);
            }

            Unbind();
        }

        public void Bind()
        {
            GlState.Current.BindSampler(this, Slot);
        }

        public void Unbind()
        {
            GlState.Current.BindSampler(0, Slot);
        }

        public override void Dispose()
        {
            if (_handle == 0)
                return;

            _gl.DeleteSampler(_handle);
            _handle = 0;

            base.Dispose();
        }

        public TextureMinFilter MinFilter { get; set; }

        public TextureMagFilter MagFilter { get; set; }

        public TextureWrapMode WrapS { get; set; }

        public TextureWrapMode WrapT { get; set; }

        public TextureWrapMode WrapR { get; set; }

        public Color BorderColor { get; set; }

        public float MinLod { get; set; }

        public float MaxLod { get; set; }

        public float LodBias { get; set; }

        public float MaxAnisotropy { get; set; }

        public DepthFunction CompareFunc { get; set; }

        public TextureCompareMode CompareMode { get; set; }

        public bool DecodeSrgb { get; set; }

        public long Version { get; set; }

        public int Slot { get; internal set; }
    }
}