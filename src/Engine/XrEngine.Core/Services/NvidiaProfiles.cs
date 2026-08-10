using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static unsafe class NvidiaProfiles
{
    private const int NvapiOk = 0;

    private const uint OglPreferDxPresentId = 0x20D690F8;
    private const uint OglForceBlitId = 0x201F619F;
    private const uint VSyncModeId = 0x00A879CF;
    private const uint OglMaxFramesAllowedId = 0x208E55E3;
    private const uint OglThreadControlId = 0x20C1221E;
    private const uint FrlFpsId = 0x10835002;

    public enum OpenGlPresentMethod : uint
    {
        Native = 0x00000000,
        Dxgi = 0x00000001,
        Auto = 0x00000002
    }

    public enum VerticalSyncMode : uint
    {
        ApplicationControlled = 0x60925292,
        ForceOff = 0x08416747,
        ForceOn = 0x47814940,
        FlipInterval2 = 0x32610244,
        FlipInterval3 = 0x71271021,
        FlipInterval4 = 0x13245256,
        Virtual = 0x18888888
    }

    private enum OglThreadControl : uint
    {
        Default = 0x00000000,
        Enable = 0x00000001,
        Disable = 0x00000002
    }

    private static readonly nint Library;

    private static readonly NvapiQueryInterface QueryInterface;

    private static readonly NvapiInitialize Initialize;
    private static readonly NvapiUnload Unload;

    private static readonly NvapiDrsCreateSession DrsCreateSession;
    private static readonly NvapiDrsDestroySession DrsDestroySession;
    private static readonly NvapiDrsLoadSettings DrsLoadSettings;
    private static readonly NvapiDrsSaveSettings DrsSaveSettings;
    private static readonly NvapiDrsGetBaseProfile DrsGetBaseProfile;
    private static readonly NvapiDrsFindProfileByName DrsFindProfileByName;
    private static readonly NvapiDrsSetSetting DrsSetSetting;
    private static readonly NvapiDrsGetSetting DrsGetSetting;

    static NvidiaProfiles()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "NVAPI is available only on Windows.");

        var libraryName = Environment.Is64BitProcess
            ? "nvapi64.dll"
            : "nvapi.dll";

        if (!NativeLibrary.TryLoad(libraryName, out Library))
        {
            throw new DllNotFoundException(
                $"{libraryName} was not found. An NVIDIA driver must be installed.");
        }

        var queryAddress = NativeLibrary.GetExport(
            Library,
            "nvapi_QueryInterface");

        QueryInterface =
            Marshal.GetDelegateForFunctionPointer<NvapiQueryInterface>(
                queryAddress);

        Initialize =
            GetFunction<NvapiInitialize>(0x0150E828);

        Unload =
            GetFunction<NvapiUnload>(0xD22BDD7E);

        DrsCreateSession =
            GetFunction<NvapiDrsCreateSession>(0x0694D52E);

        DrsDestroySession =
            GetFunction<NvapiDrsDestroySession>(0xDAD9CFF8);

        DrsLoadSettings =
            GetFunction<NvapiDrsLoadSettings>(0x375DBD6B);

        DrsSaveSettings =
            GetFunction<NvapiDrsSaveSettings>(0xFCBC7E14);

        DrsGetBaseProfile =
            GetFunction<NvapiDrsGetBaseProfile>(0xDA8466A0);

        DrsFindProfileByName =
            GetFunction<NvapiDrsFindProfileByName>(0x7E4A9A0B);

        DrsSetSetting =
            GetFunction<NvapiDrsSetSetting>(0x577DD202);

        DrsGetSetting =
            GetFunction<NvapiDrsGetSetting>(0x73BF8338);

        Debug.Assert(sizeof(NvDrsSettingValue) == 4100);
        Debug.Assert(sizeof(NvDrsSetting) == 12320);

        Debug.Assert(
            Marshal.OffsetOf<NvDrsSetting>(
                nameof(NvDrsSetting.SettingId)).ToInt32() == 4100);

        Debug.Assert(
            Marshal.OffsetOf<NvDrsSetting>(
                nameof(NvDrsSetting.PredefinedValue)).ToInt32() == 4120);

        Debug.Assert(
            Marshal.OffsetOf<NvDrsSetting>(
                nameof(NvDrsSetting.CurrentValue)).ToInt32() == 8220);
    }

    /// <summary>
    /// Disables NVIDIA OpenGL Threaded Optimization.
    ///
    /// A null profile name modifies the global/base profile.
    /// Otherwise, the profile must already exist.
    /// </summary>
    public static void DisableOpenGlThreadedOptimization(string? profileName = null)
    {
        SetDwordSetting(OglThreadControlId, (uint)OglThreadControl.Disable, profileName);
    }

    public static void SetOpenGlPresentMethod(OpenGlPresentMethod method, string? profileName = null)
    {
        SetDwordSetting(OglPreferDxPresentId, (uint)method, profileName);
    }

    public static OpenGlPresentMethod GetOpenGlPresentMethod(string? profileName = null)
    {
        return (OpenGlPresentMethod)GetDwordSetting(OglPreferDxPresentId, profileName);
    }

    public static void SetOpenGlForceBlit(bool enabled, string? profileName = null)
    {
        SetDwordSetting(OglForceBlitId, enabled ? 1u : 0u, profileName);
    }

    public static bool GetOpenGlForceBlit(string? profileName = null)
    {
        return GetDwordSetting(OglForceBlitId, profileName) != 0;
    }

    public static void SetVerticalSyncMode(VerticalSyncMode mode, string? profileName = null)
    {
        SetDwordSetting(VSyncModeId, (uint)mode, profileName);
    }

    public static VerticalSyncMode GetVerticalSyncMode(string? profileName = null)
    {
        return (VerticalSyncMode)GetDwordSetting(VSyncModeId, profileName);
    }

    public static void DisableFrameRateLimiter(string? profileName = null)
    {
        SetDwordSetting(FrlFpsId, 0, profileName);
    }

    public static void SetFrameRateLimit(uint fps, string? profileName = null)
    {
        if (fps > 0x3ff)
            throw new ArgumentOutOfRangeException(nameof(fps), "NVIDIA FRL supports values up to 1023 FPS.");

        SetDwordSetting(FrlFpsId, fps, profileName);
    }

    public static uint GetFrameRateLimit(string? profileName = null)
    {
        return GetDwordSetting(FrlFpsId, profileName);
    }

    public static uint GetOpenGlMaxFramesAllowed(string? profileName = null)
    {
        return GetDwordSetting(OglMaxFramesAllowedId, profileName);
    }

    public static void SetOpenGlMaxFramesAllowed(uint value, string? profileName = null)
    {
        SetDwordSetting(OglMaxFramesAllowedId, value, profileName);
    }

    private static void SetDwordSetting(uint settingId, uint value, string? profileName)
    {
        Check(Initialize(), nameof(Initialize));
        nint session = 0;

        try
        {
            Check(DrsCreateSession(out session), nameof(DrsCreateSession));
            Check(DrsLoadSettings(session), nameof(DrsLoadSettings));
            var profile = GetProfile(session, profileName);

            NvDrsSetting setting = default;
            setting.Version = (uint)sizeof(NvDrsSetting) | (1u << 16);
            setting.SettingId = settingId;
            setting.SettingType = NvDrsSettingType.Dword;
            setting.CurrentValue.Dword = value;

            Check(DrsSetSetting(session, profile, &setting), nameof(DrsSetSetting));
            Check(DrsSaveSettings(session), nameof(DrsSaveSettings));
        }
        finally
        {
            if (session != 0)
                DrsDestroySession(session);

            Unload();
        }
    }

    private static uint GetDwordSetting(uint settingId, string? profileName)
    {
        Check(Initialize(), nameof(Initialize));
        nint session = 0;

        try
        {
            Check(DrsCreateSession(out session), nameof(DrsCreateSession));
            Check(DrsLoadSettings(session), nameof(DrsLoadSettings));
            var profile = GetProfile(session, profileName);

            NvDrsSetting setting = default;
            setting.Version = (uint)sizeof(NvDrsSetting) | (1u << 16);

            Check(DrsGetSetting(session, profile, settingId, &setting), nameof(DrsGetSetting));

            if (setting.SettingType != NvDrsSettingType.Dword)
                throw new InvalidOperationException($"NVAPI setting 0x{settingId:X8} is not a DWORD setting.");

            return setting.CurrentValue.Dword;
        }
        finally
        {
            if (session != 0)
                DrsDestroySession(session);

            Unload();
        }
    }

    private static nint GetProfile(nint session, string? profileName)
    {
        if (profileName is null)
        {
            Check(DrsGetBaseProfile(session, out var profile), nameof(DrsGetBaseProfile));
            return profile;
        }

        Check(DrsFindProfileByName(session, profileName, out var namedProfile), $"{nameof(DrsFindProfileByName)}(\"{profileName}\")");
        return namedProfile;
    }

    private static T GetFunction<T>(uint id)
        where T : Delegate
    {
        var address = QueryInterface(id);

        if (address == 0)
        {
            throw new EntryPointNotFoundException(
                $"NVAPI function 0x{id:X8} is unavailable.");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static void Check(int status, string operation)
    {
        if (status == NvapiOk)
            return;

        throw new Win32Exception(
            status,
            $"{operation} failed with NVAPI status {status}.");
    }

    private enum NvDrsSettingType : uint
    {
        Dword = 0,
        Binary = 1,
        String = 2,
        WideString = 3,
        Qword = 4
    }

    private enum NvDrsSettingLocation : uint
    {
        CurrentProfile = 0,
        GlobalProfile = 1,
        BaseProfile = 2,
        DefaultProfile = 3
    }

    /*
     * Native NVDRS_BINARY_SETTING:
     *
     * NvU32 valueLength;
     * NvU8  valueData[4096];
     *
     * The union is therefore 4100 bytes, because this is its
     * largest member.
     */
    [StructLayout(LayoutKind.Explicit, Size = 4100, Pack = 4)]
    private struct NvDrsSettingValue
    {
        [FieldOffset(0)]
        public uint Dword;

        [FieldOffset(0)]
        public ulong Qword;

        [FieldOffset(0)]
        public uint BinaryLength;

        [FieldOffset(4)]
        public fixed byte BinaryData[4096];

        [FieldOffset(0)]
        public fixed char WideString[2048];
    }

    /*
     * Native NVDRS_SETTING_V1.
     *
     * Offsets:
     *
     *     0  Version
     *     4  SettingName
     *  4100  SettingId
     *  4104  SettingType
     *  4108  SettingLocation
     *  4112  IsCurrentPredefined
     *  4116  IsPredefinedValid
     *  4120  PredefinedValue
     *  8220  CurrentValue
     *
     * Total: 12320 bytes / 0x3020.
     */
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct NvDrsSetting
    {
        public uint Version;

        public fixed char SettingNameBuf[2048];

        public uint SettingId;
        public NvDrsSettingType SettingType;
        public NvDrsSettingLocation SettingLocation;

        public uint IsCurrentPredefined;
        public uint IsPredefinedValid;

        public NvDrsSettingValue PredefinedValue;
        public NvDrsSettingValue CurrentValue;

        public string SettingName
        {
            get
            {
                fixed (char* pChar = SettingNameBuf)
                    return new string(pChar, 0, new ReadOnlySpan<char>(pChar, 2048).IndexOf('\0'));
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint NvapiQueryInterface(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiInitialize();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiUnload();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsCreateSession(
        out nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsDestroySession(
        nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsLoadSettings(
        nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsSaveSettings(
        nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsGetBaseProfile(
        nint session,
        out nint profile);

    [UnmanagedFunctionPointer(
        CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    private delegate int NvapiDrsFindProfileByName(
        nint session,
        [MarshalAs(UnmanagedType.LPWStr)] string profileName,
        out nint profile);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsSetSetting(
        nint session,
        nint profile,
        NvDrsSetting* setting);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsGetSetting(
        nint session,
        nint profile,
        uint settingId,
        NvDrsSetting* setting);
}