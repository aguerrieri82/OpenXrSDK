using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace XrEngine
{
    public static class Utils
    {
        public static uint ComputeMaxMipLevel(int width, int height, int minSize)
        {
            var size = Math.Max(width, height);
            uint level = 0;

            while (size > minSize)
            {
                size >>= 1;
                level++;
            }

            return level;
        }

        public static Guid HashGuid(string text)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(text));
            return new Guid(MD5.HashData(Encoding.UTF8.GetBytes(text)));
        }

        public unsafe static bool ArrayEquals<T>(T[] a, T[] b) where T : unmanaged
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            var nint = len * sizeof(T) / 4;
            fixed (T* pa = a, pb = b)
            {
                var intA = (int*)pa;
                var intB = (int*)pb;
                while (nint > 0)
                {
                    if (*intA != *intB)
                        return false;
                    intA++;
                    intB++;
                    nint--;
                }
            }
            return true;
        }

        public static bool ArrayEquals(int[] a, int[] b)
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            for (var i = 0; i < len; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }


        public static T CreateInstance<T>(Type actualType)
        {
            var ctor = actualType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));

            if (ctor == null)
                return Activator.CreateInstance<T>();

            var args = ctor.GetParameters()
                .Select(p => p.DefaultValue)
                .ToArray();

            var instance = ctor.Invoke(args);
            return (T)instance;
        }
    }
}
