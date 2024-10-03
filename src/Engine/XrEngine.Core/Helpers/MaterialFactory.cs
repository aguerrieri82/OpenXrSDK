using XrMath;

namespace XrEngine
{
    public static class MaterialFactory
    {
        public static IPbrMaterial CreatePbr(Color color)
        {
            var result = CreatePbr<PbrV2Material>();
            result.Color = color;
            result.Metalness = 0;
            result.Roughness = 0.5f;
            return result;
        }

        public static T CreatePbr<T>() where T : IPbrMaterial, new()
        {
            return new T();
        }


        public static Type DefaultPbr => typeof(PbrV2Material);
    }
}
