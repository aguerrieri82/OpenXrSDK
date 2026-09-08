using System.Runtime.InteropServices;

namespace Oculus.Platform;

/// <summary>
/// Windows standalone Platform C API. Uses the ovr message queue, independently of Core's hzpsdk API.
/// Declarations follow OVR_Platform.h and the associated message, user and error headers.
/// </summary>
public static class StandaloneNative
{
    private const string Library = "LibOVRPlatformImpl64_1";

    [StructLayout(LayoutKind.Sequential)]
    public struct OculusInitParams
    {
        public int Type;
        [MarshalAs(UnmanagedType.LPUTF8Str)] public string Email;
        [MarshalAs(UnmanagedType.LPUTF8Str)] public string Password;
        public ulong AppId;
        [MarshalAs(UnmanagedType.LPUTF8Str)] public string UriPrefixOverride;
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_Platform_InitializeStandaloneOculus(ref OculusInitParams init);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ovr_PopMessage();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ovr_FreeMessage(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_Message_GetRequestID(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ovr_Message_IsError(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetError(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ovr_Error_GetCode(IntPtr error);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPlatformInitialize(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ovr_PlatformInitialize_GetResult(IntPtr initialization);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLoggedInUser();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUser(IntPtr message);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetID(IntPtr user);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetAccessToken();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetString(IntPtr message);
}
