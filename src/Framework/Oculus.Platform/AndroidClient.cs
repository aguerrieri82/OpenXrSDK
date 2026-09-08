#if ANDROID
using Java.Lang;
using JClass = Java.Lang.Class;
using JObject = Java.Lang.Object;

namespace Oculus.Platform;

/// <summary>Horizon OS supplement bridge using .NET for Android instead of Unity Java wrappers.</summary>
internal static class AndroidClient
{
    private static JObject _bridge;
    private static JClass _bridgeClass;
    private static ulong _globalSession;

    internal static HorizonStatus Initialize(global::Android.App.Activity activity, string appId)
    {
        if (_bridge != null) return new HorizonStatus(0, "Already initialized");
        var loader = activity.ClassLoader;
        var bridgeClass = loader.LoadClass("horizonos.supplement.hzplatformclientcore.HorizonPlatformCore");
        using var configClass = loader.LoadClass("horizonos.supplement.hzplatformclientcore.AndroidHorizonPlatformConfig");
        using var contextClass = JClass.FromType(typeof(global::Android.Content.Context));
        using var stringClass = JClass.FromType(typeof(Java.Lang.String));
        using var appIdString = new Java.Lang.String(appId);
        using var constructor = configClass.GetConstructor(contextClass, stringClass);
        using var config = constructor.NewInstance(activity, appIdString);
        using var bridgeConstructor = bridgeClass.GetConstructor();
        var bridge = bridgeConstructor.NewInstance();
        try
        {
            using var setup = bridgeClass.GetMethod("setup", configClass);
            using var ignored = setup.Invoke(bridge, config);
            using var getSession = bridgeClass.GetMethod("getGlobalSessionId");
            using var session = getSession.Invoke(bridge);
            _globalSession = unchecked((ulong)((Java.Lang.Long)session).LongValue());
            _bridgeClass = bridgeClass;
            _bridge = bridge;
            return new HorizonStatus(0, "Initialize");
        }
        catch
        {
            bridge.Dispose();
            bridgeClass.Dispose();
            throw;
        }
    }

    internal static ulong GlobalSession() => _globalSession;

    private static JObject Invoke(string name, JClass[] types, params JObject[] args)
    {
        if (_bridge == null) throw new InvalidOperationException(Core.PlatformUninitializedError);
        using var method = _bridgeClass.GetMethod(name, types);
        return method.Invoke(_bridge, args);
    }

    internal static int GetMessageCount(ulong sessionId)
    {
        using var id = Java.Lang.Long.ValueOf(unchecked((long)sessionId));
        using var result = Invoke("getMessageCount", [Java.Lang.Long.Type], id);
        return ((Java.Lang.Integer)result).IntValue();
    }

    private static ulong Send(string method, string module, string requestName, int apiVersion, string requestData, int cookie)
    {
        using var stringClass = JClass.FromType(typeof(Java.Lang.String));
        using var moduleArg = new Java.Lang.String(module);
        using var nameArg = new Java.Lang.String(requestName);
        using var versionArg = Java.Lang.Integer.ValueOf(apiVersion);
        using var dataArg = new Java.Lang.String(requestData);
        using var cookieArg = Java.Lang.Integer.ValueOf(cookie);
        using var result = Invoke(method,
            [stringClass, stringClass, Java.Lang.Integer.Type, stringClass, Java.Lang.Integer.Type],
            moduleArg, nameArg, versionArg, dataArg, cookieArg);
        return unchecked((ulong)((Java.Lang.Long)result).LongValue());
    }

    internal static ulong MakeRequest(string module, string requestName, int version, string data, int cookie)
        => Send("makeRequest", module, requestName, version, data, cookie);
    internal static ulong MakeSession(string module, string requestName, int version, string data, int cookie)
        => Send("makeSession", module, requestName, version, data, cookie);

    internal static void StopSession(ulong sessionId)
    {
        using var id = Java.Lang.Long.ValueOf(unchecked((long)sessionId));
        using var ignored = Invoke("stopSession", [Java.Lang.Long.Type], id);
    }

    internal static Message PopMessage(ulong sessionId, bool yield)
    {
        using var id = Java.Lang.Long.ValueOf(unchecked((long)sessionId));
        using var shouldYield = Java.Lang.Boolean.ValueOf(yield);
        using var message = Invoke("popMessage", [Java.Lang.Long.Type, Java.Lang.Boolean.Type], id, shouldYield);
        if (message == null) return null;
        using var type = message.Class;
        using var requestIdField = type.GetField("requestId");
        using var statusCodeField = type.GetField("statusCode");
        using var cookieField = type.GetField("cookie");
        using var statusField = type.GetField("statusMessage");
        using var dataField = type.GetField("data");
        using var status = statusField.Get(message);
        using var data = dataField.Get(message);
        return new Message(unchecked((ulong)requestIdField.GetLong(message)), sessionId,
            cookieField.GetInt(message), data?.ToString(),
            new HorizonStatus(statusCodeField.GetInt(message), status?.ToString() ?? ""));
    }
}
#endif
