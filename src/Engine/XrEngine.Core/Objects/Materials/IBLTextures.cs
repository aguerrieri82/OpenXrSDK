namespace XrEngine
{
    public class IBLTextures : IDisposable
    {
        public void Dispose()
        {
            LambertianEnv?.Dispose();
            LambertianEnv = null;

            GGXEnv?.Dispose();
            GGXEnv = null;

            GGXLUT?.Dispose();
            GGXLUT = null;

            CharlieEnv?.Dispose();
            CharlieEnv = null;

            CharlieLUT?.Dispose();
            CharlieLUT = null;

            Env?.Dispose();
            Env = null;
        }

        public TextureCube? LambertianEnv;

        public TextureCube? GGXEnv;
        public Texture2D? GGXLUT;

        public TextureCube? CharlieEnv;
        public Texture2D? CharlieLUT;

        public TextureCube? Env;

        public uint MipCount;
    }
}
