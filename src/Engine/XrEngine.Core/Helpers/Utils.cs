using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace XrEngine
{
    public static class Utils
    {
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

        public unsafe static bool ArrayEquals(int[] a, int[] b)
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            for (var i = 0; i < len; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}
