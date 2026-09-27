

namespace XrEngine.Music
{
    public class SystemTimeClock : IReferenceClock
    {
        public ulong Now => EngineNativeLib.Now();
    }
}
