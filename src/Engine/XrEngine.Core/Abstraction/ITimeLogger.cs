using XrMath;

namespace XrEngine
{
    public interface ITimeLogger
    {
        void LogValue<T>(string name, T value);

        void Checkpoint(string name, Color color);

        void Clear();
    }
}
