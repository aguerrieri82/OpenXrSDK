using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public static class StreamExtensions
    {
        public unsafe static T ReadStruct<T>(this Stream stream) where T : unmanaged
        {
            var buffer = stackalloc T[1];

            var span = new Span<byte>((byte*)buffer, sizeof(T));
            
            stream.Read(span);

            return *buffer;
        }

        public unsafe static MemoryStream ToMemory(this Stream stream)
        {
            var result = new MemoryStream();
            stream.CopyTo(result);
            result.Position = 0;
            stream.Dispose();
            return result;
        }
    }
}
