
#if ANDROID


using Android.Content;

namespace XrInteraction
{
    public interface IMainActivity
    {
        void StartActivityForResult(Intent intent, int reqCode, Action<Result, Intent?> onResult);
    }
}

#endif