﻿using XrMath;

namespace XrEngine
{
    public static class MaterialFactory
    {
        static MaterialFactory()
        {
            DefaultPbr = typeof(PbrV2Material);
        }

        public static IPbrMaterial CreatePbr(Color color)
        {
            var result = CreatePbr(DefaultPbr);
            result.Color = color;
            result.Metalness = 0;
            result.Roughness = 0.5f;
            return result;
        }

        public static IPbrMaterial CreatePbr(Texture2D color)
        {
            var result = CreatePbr(DefaultPbr);
            result.Color = Color.White;
            result.ColorMap = color;
            result.Metalness = 0;
            result.Roughness = 0.5f;
            return result;
        }

        static IPbrMaterial CreatePbr(Type type)
        {
            return (IPbrMaterial)Activator.CreateInstance(type)!;
        }

        public static T CreatePbr<T>() where T : IPbrMaterial, new()
        {
            return new T();
        }


        public static Type DefaultPbr { get; set; }
    }
}
