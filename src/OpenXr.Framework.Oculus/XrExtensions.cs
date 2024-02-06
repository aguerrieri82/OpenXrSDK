using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public static unsafe class XrExtensions
    {
        public static UuidEXT[] GetWalls(this RoomLayoutFB layout)
        {
            var span = new Span<UuidEXT>(layout.WallUuids, (int)layout.WallUuidCountOutput);
            return span.ToArray();
        }

        public static Guid ToGuid(this UuidEXT uuid)
        {
            return new Guid(new Span<byte>(uuid.Data, 16));
        }
    }
}
