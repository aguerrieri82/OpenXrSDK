using Silk.NET.OpenXR;

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
