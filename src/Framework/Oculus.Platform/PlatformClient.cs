namespace Oculus.Platform;

public static class PlatformClient
{
    public static ulong GlobalSession()
    {
#if ANDROID
        return AndroidClient.GlobalSession();
#else
        return 0;
#endif
    }

    public static int GetResponseCount() => GetMessageCount(GlobalSession());
    public static Message PopResponse(bool yield) => PopMessage(GlobalSession(), yield);

    public static int GetMessageCount(ulong sessionID)
    {
#if ANDROID
        return AndroidClient.GetMessageCount(sessionID);
#else
        return WindowsClient.GetMessageCount(sessionID);
#endif
    }

    public static Message PopMessage(ulong sessionID, bool yield)
    {
#if ANDROID
        return AndroidClient.PopMessage(sessionID, yield);
#else
        return WindowsClient.PopMessage(sessionID, yield);
#endif
    }

    public static ulong MakeRequest(string module, string requestName, int apiVersion, string requestData, int cookie)
    {
        if (!Core.IsInitialized()) throw new InvalidOperationException(Core.PlatformUninitializedError);
#if ANDROID
        return AndroidClient.MakeRequest(module, requestName, apiVersion, requestData, cookie);
#else
        return WindowsClient.MakeRequest(module, requestName, apiVersion, requestData, cookie);
#endif
    }

    public static ulong MakeSession(string module, string requestName, int apiVersion, string requestData, int cookie)
    {
#if ANDROID
        return AndroidClient.MakeSession(module, requestName, apiVersion, requestData, cookie);
#else
        throw new PlatformNotSupportedException("The upstream Windows transport does not support notification sessions.");
#endif
    }

    public static void StopSession(ulong sessionID)
    {
#if ANDROID
        AndroidClient.StopSession(sessionID);
#else
        throw new PlatformNotSupportedException("The upstream Windows transport does not support notification sessions.");
#endif
    }
}
