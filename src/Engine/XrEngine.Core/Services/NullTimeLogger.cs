using XrMath;

namespace XrEngine
{
    public class NullTimeLogger : ITimeLogger
    {
        NullTimeLogger()
        {

        }

        public void Checkpoint(string name, Color color)
        {

        }

        public void Clear()
        {

        }

        public void LogValue<T>(string name, T value)
        {

        }

        public static readonly NullTimeLogger Instance = new(); 
    }
}
