namespace XrEngine.Reconstruct
{
    public partial class MultiTextureMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;
        private int _activeExposure;

        static MultiTextureMaterial()
        {
            SHADER = new StandardShader()
            {
                FragmentSourceName = "[XrEngine.Reconstruct]multi_tex.frag",
            };
        }

        public MultiTextureMaterial()
        {
            _shader = SHADER;
            Exposure = [];
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("TANGENT_AS_CONST");
            bld.AddFeature("HAS_TANGENTS");
            bld.AddFeature("HAS_UV2");

            bld.AddFeature($"IMG_COUNT {Texture?.Depth}");

            if (Exposure.Length > 0)
                bld.AddFeature("USE_EXPOSURE");

            if (MixColors)
                bld.AddFeature("MIX_COLORS");

            bld.PrepareTexture(Texture);

            bld.ExecuteAction((ctx, up) =>
            {
                up.LoadTextureFixSrgb(ctx, Texture!, 1);

                if (Exposure.Length > 0)
                    up.SetUniform("uExposure", Exposure);
            });

            base.UpdateShaderMaterial(bld);
        }

        [Range(0, 1, 0.01f)]
        [Notify(ChangeType.Render)]
        public partial float[] Exposure { get; set; }

        public Texture2D? Texture { get; set; }

        [Notify(ChangeType.Render)]
        public partial bool MixColors { get; set; }

        public int ActiveEsposure
        {
            get => _activeExposure;
            set
            {
                _activeExposure = value;
                NotifyChanged(new ObjectChange(ChangeType.Property, this, [nameof(ActiveEsposureValue)]));
            }
        }

        [Range(-1, 1, 0.01f)]
        public float ActiveEsposureValue
        {
            get => Exposure[ActiveEsposure];
            set => Exposure[ActiveEsposure] = value;
        }
    }
}
