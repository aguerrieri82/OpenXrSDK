using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class OpenXrException : Exception
    {
        public OpenXrException(Result result, string message)
            : base($"OpenXr error: {result} ({message})")
        {
            Result = result;
        }


        public Result Result { get; }
    }
}
