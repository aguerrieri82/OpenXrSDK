
using System.Diagnostics;
using System.Text;

namespace XrEngine.Helpers
{
    public static class TextureSamplerFactory
    {
        class TextureSamplerBind
        {
            public TextureSampler? Sampler;

            public long LastVersion;
        }

        static Dictionary<Texture, TextureSamplerBind> _srgbSamplers = [];


        public static TextureSampler DisableSrgbDecode(Texture texture)
        {
            Debug.Assert(texture.Sampler == null);

            if (!_srgbSamplers.TryGetValue(texture, out var bind))
            {
                bind = new TextureSamplerBind
                {
                    Sampler = new TextureSampler()
                    {
                        DecodeSrgb = false
                    },
                    LastVersion = -1
                };
                _srgbSamplers[texture] = bind;
            }

            if (bind.LastVersion != texture.Version)
            {
                bind.Sampler!.Update(texture);
                bind.LastVersion = texture.Version;
            }

            return bind.Sampler!;
        }
    }
}
