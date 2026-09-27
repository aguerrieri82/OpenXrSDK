
using OpenAl.Framework;

namespace XrEngine.Music
{
    public class AlClock : IReferenceClock
    {
        public ulong Now => AlDevice.Current!.Clock;
    }
}
