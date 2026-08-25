namespace XrEngine
{
    public static class TextureSlots
    {
        public static readonly ResourceSlot Texture1 = new(nameof(Texture1));

        public static readonly ResourceSlot Albedo = new(nameof(Albedo));
        public static readonly ResourceSlot Normal = new(nameof(Normal));
        public static readonly ResourceSlot MetallicRoughness = new(nameof(MetallicRoughness));
        public static readonly ResourceSlot SpecularGlossiness = new(nameof(SpecularGlossiness));
        public static readonly ResourceSlot Occlusion = new(nameof(Occlusion));
        public static readonly ResourceSlot Emissive = new(nameof(Emissive));

        public static readonly ResourceSlot PlanarReflection = new(nameof(PlanarReflection));
        public static readonly ResourceSlot HeightMap = new(nameof(HeightMap));
        public static readonly ResourceSlot Morph = new(nameof(Morph));
        public static readonly ResourceSlot ProjDepth = new(nameof(ProjDepth));
        public static readonly ResourceSlot TransmissionMap = new(nameof(TransmissionMap));
        public static readonly ResourceSlot ThicknessMap = new(nameof(ThicknessMap));
        public static readonly ResourceSlot IridescenceMap = new(nameof(IridescenceMap));
        public static readonly ResourceSlot IridescenceThicknessMap = new(nameof(IridescenceThicknessMap));

        public static readonly ResourceSlot SheenColor = new(nameof(SheenColor));
        public static readonly ResourceSlot SheenRoughness = new(nameof(SheenRoughness));

        public static readonly ResourceSlot ClearCoat = new(nameof(ClearCoat));
        public static readonly ResourceSlot ClearCoatNormal = new(nameof(ClearCoatNormal));
        public static readonly ResourceSlot ClearCoatRoughness = new(nameof(ClearCoatRoughness));

        public static readonly ResourceSlot Specular = new(nameof(Specular));
        public static readonly ResourceSlot SpecularColor = new(nameof(SpecularColor));

        public static readonly ResourceSlot VolumeBackground = new(4, nameof(VolumeBackground));
        public static readonly ResourceSlot VolumeForeground = new(5, nameof(VolumeForeground));
        public static readonly ResourceSlot EnvDepth = new(6, nameof(EnvDepth));
        public static readonly ResourceSlot ShadowMap = new(7, nameof(ShadowMap));
        public static readonly ResourceSlot IblGgxLut = new(8, nameof(IblGgxLut));
        public static readonly ResourceSlot IblLambertianEnv = new(9, nameof(IblLambertianEnv));
        public static readonly ResourceSlot IblGgxEnv = new(10, nameof(IblGgxEnv));

        public static readonly ResourceSlot IblCharlieEnv = new(12, nameof(IblCharlieEnv));
        public static readonly ResourceSlot CharlieLut = new(13, nameof(CharlieLut));


        public static readonly SlotMask Reserved = ResourceSlot.FillMask(typeof(TextureSlots));
    }
}