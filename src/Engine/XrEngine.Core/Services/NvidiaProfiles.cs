using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

public unsafe class NvidiaProfiles : IDisposable
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

    private nint _library;
    private nint _session;
    private nint _profile;
    private bool _initialized;
    private bool _disposed;

    private NvapiQueryInterface? _queryInterface;
    private NvapiInitialize? _initialize;
    private NvapiUnload? _unload;
    private NvapiDrsCreateSession? _drsCreateSession;
    private NvapiDrsDestroySession? _drsDestroySession;
    private NvapiDrsLoadSettings? _drsLoadSettings;
    private NvapiDrsSaveSettings? _drsSaveSettings;
    private NvapiDrsFindProfileByName? _drsFindProfileByName;
    private NvapiDrsCreateProfile? _drsCreateProfile;
    private NvapiDrsFindApplicationByName? _drsFindApplicationByName;
    private NvapiDrsCreateApplication? _drsCreateApplication;
    private NvapiDrsSetSetting? _drsSetSetting;
    private NvapiDrsGetSetting? _drsGetSetting;

    private NvapiDrsRestoreAllDefaults? _drsRestoreAllDefaults;

    public string ProfileName => Process.GetCurrentProcess().ProcessName;

    public string ExecutableName
    {
        get
        {
            var path = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Unable to determine current executable.");

            return Path.GetFileName(path);
        }
    }

    public void DisableOpenGlThreadedOptimization()
    {
        SetDwordSetting(OglThreadControlId, (uint)OglThreadControl.Disable);
    }

    public void SetOpenGlPresentMethod(OpenGlPresentMethod method)
    {
        SetDwordSetting(OglPreferDxPresentId, (uint)method);
    }

    public OpenGlPresentMethod GetOpenGlPresentMethod()
    {
        return (OpenGlPresentMethod)GetDwordSetting(OglPreferDxPresentId);
    }

    public void SetOpenGlForceBlit(bool enabled)
    {
        SetDwordSetting(OglForceBlitId, enabled ? 1u : 0u);
    }

    public bool GetOpenGlForceBlit()
    {
        return GetDwordSetting(OglForceBlitId) != 0;
    }

    public void SetVerticalSyncMode(VerticalSyncMode mode)
    {
        SetDwordSetting(VSyncModeId, (uint)mode);
    }

    public VerticalSyncMode GetVerticalSyncMode()
    {
        return (VerticalSyncMode)GetDwordSetting(VSyncModeId);
    }

    public void DisableFrameRateLimiter()
    {
        SetDwordSetting(FrlFpsId, 0);
    }

    public void SetFrameRateLimit(uint fps)
    {
        if (fps > 0x3ff)
            throw new ArgumentOutOfRangeException(nameof(fps), "NVIDIA FRL supports values up to 1023 FPS.");

        SetDwordSetting(FrlFpsId, fps);
    }

    public uint GetFrameRateLimit()
    {
        return GetDwordSetting(FrlFpsId);
    }

    public uint GetOpenGlMaxFramesAllowed()
    {
        return GetDwordSetting(OglMaxFramesAllowedId);
    }

    public void SetOpenGlMaxFramesAllowed(uint value)
    {
        SetDwordSetting(OglMaxFramesAllowedId, value);
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_initialized)
            return;

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("NVAPI is available only on Windows.");

        try
        {
            var libraryName = Environment.Is64BitProcess ? "nvapi64.dll" : "nvapi.dll";

            if (!NativeLibrary.TryLoad(libraryName, out _library))
                throw new DllNotFoundException($"{libraryName} was not found. An NVIDIA driver must be installed.");

            var queryAddress = NativeLibrary.GetExport(_library, "nvapi_QueryInterface");
            _queryInterface = Marshal.GetDelegateForFunctionPointer<NvapiQueryInterface>(queryAddress);

            _initialize = GetFunction<NvapiInitialize>(0x0150E828);
            _unload = GetFunction<NvapiUnload>(0xD22BDD7E);

            _drsCreateSession = GetFunction<NvapiDrsCreateSession>(0x0694D52E);
            _drsDestroySession = GetFunction<NvapiDrsDestroySession>(0xDAD9CFF8);
            _drsLoadSettings = GetFunction<NvapiDrsLoadSettings>(0x375DBD6B);
            _drsSaveSettings = GetFunction<NvapiDrsSaveSettings>(0xFCBC7E14);

            _drsFindProfileByName = GetFunction<NvapiDrsFindProfileByName>(0x7E4A9A0B);
            _drsCreateProfile = GetFunction<NvapiDrsCreateProfile>(0xCC176068);
            _drsFindApplicationByName = GetFunction<NvapiDrsFindApplicationByName>(0xEEE566B2);
            _drsCreateApplication = GetFunction<NvapiDrsCreateApplication>(0x4347A9DE);

            _drsSetSetting = GetFunction<NvapiDrsSetSetting>(0x577DD202);
            _drsGetSetting = GetFunction<NvapiDrsGetSetting>(0x73BF8338);

            _drsRestoreAllDefaults = GetFunction<NvapiDrsRestoreAllDefaults>(0x5927B094);

            Debug.Assert(sizeof(NvDrsSettingValue) == 4100);
            Debug.Assert(sizeof(NvDrsSetting) == 12320);
            Debug.Assert(Marshal.OffsetOf<NvDrsSetting>(nameof(NvDrsSetting.SettingId)).ToInt32() == 4100);
            Debug.Assert(Marshal.OffsetOf<NvDrsSetting>(nameof(NvDrsSetting.PredefinedValue)).ToInt32() == 4120);
            Debug.Assert(Marshal.OffsetOf<NvDrsSetting>(nameof(NvDrsSetting.CurrentValue)).ToInt32() == 8220);

            Check(_initialize(), nameof(_initialize));
            Check(_drsCreateSession(out _session), nameof(_drsCreateSession));
            Check(_drsLoadSettings(_session), nameof(_drsLoadSettings));

            _profile = ResolveApplicationProfile();

            if (_profile == 0)
                throw new InvalidOperationException("NVAPI returned a null application profile.");

            _initialized = true;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private nint ResolveApplicationProfile()
    {
        var executableName = ExecutableName;
        var executableNameValue = NvApiUnicodeString.Create(executableName);

        NvDrsApplication application = default;
        application.Version = MakeVersion<NvDrsApplication>(4);

        var status = _drsFindApplicationByName!(_session, executableNameValue, out var profile, &application);

        if (status == NvapiOk)
            return profile;

        // No existing application association: use the process name as the new profile name.
        var profileNameValue = NvApiUnicodeString.Create(ProfileName);

        status = _drsFindProfileByName!(_session, profileNameValue, out profile);

        if (status != NvapiOk)
        {
            NvDrsProfile profileInfo = default;
            profileInfo.Version = MakeVersion<NvDrsProfile>(1);
            profileInfo.ProfileName = profileNameValue;

            Check(_drsCreateProfile!(_session, &profileInfo, out profile), nameof(_drsCreateProfile));
        }

        if (profile == 0)
            throw new InvalidOperationException("NVAPI returned a null profile.");

        application = default;
        application.Version = MakeVersion<NvDrsApplication>(4);
        application.AppName = executableNameValue;
        application.UserFriendlyName = profileNameValue;

        Check(_drsCreateApplication!(_session, profile, &application), nameof(_drsCreateApplication));
        Check(_drsSaveSettings!(_session), nameof(_drsSaveSettings));

        return profile;
    }

    private void SetDwordSetting(uint settingId, uint value)
    {
        EnsureInitialized();

        NvDrsSetting setting = default;
        setting.Version = MakeVersion<NvDrsSetting>(1);
        setting.SettingId = settingId;
        setting.SettingType = NvDrsSettingType.Dword;
        setting.CurrentValue.Dword = value;

        Check(_drsSetSetting!(_session, _profile, &setting), nameof(_drsSetSetting));
        Check(_drsSaveSettings!(_session), nameof(_drsSaveSettings));
    }

    private uint GetDwordSetting(uint settingId)
    {
        EnsureInitialized();

        NvDrsSetting setting = default;
        setting.Version = MakeVersion<NvDrsSetting>(1);

        Check(_drsGetSetting!(_session, _profile, settingId, &setting), nameof(_drsGetSetting));

        if (setting.SettingType != NvDrsSettingType.Dword)
            throw new InvalidOperationException($"NVAPI setting 0x{settingId:X8} is not a DWORD setting.");

        return setting.CurrentValue.Dword;
    }

    public void RestoreAllDefaults()
    {
        EnsureInitialized();

        Check(_drsRestoreAllDefaults!(_session), nameof(_drsRestoreAllDefaults));
        Check(_drsSaveSettings!(_session), nameof(_drsSaveSettings));

        _profile = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_session != 0)
        {
            _drsDestroySession?.Invoke(_session);
            _session = 0;
            _profile = 0;
        }

        _unload?.Invoke();

        if (_library != 0)
        {
            NativeLibrary.Free(_library);
            _library = 0;
        }

        _initialized = false;
        GC.SuppressFinalize(this);
    }

    private T GetFunction<T>(uint id) where T : Delegate
    {
        var address = _queryInterface!(id);

        if (address == 0)
            throw new EntryPointNotFoundException($"NVAPI function 0x{id:X8} is unavailable.");

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static uint MakeVersion<T>(uint version) where T : unmanaged
    {
        return (uint)sizeof(T) | (version << 16);
    }

    private static void Check(int status, string operation)
    {
        if (status == NvapiOk)
            return;

        throw new Win32Exception(status, $"{operation} failed with NVAPI status {status}.");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NvApiUnicodeString
    {
        public fixed char Data[2048];

        public static NvApiUnicodeString Create(string value)
        {
            if (value.Length >= 2048)
                throw new ArgumentOutOfRangeException(nameof(value), "NVAPI Unicode strings are limited to 2047 characters.");

            NvApiUnicodeString result = default;

            fixed (char* src = value)
            {
                Buffer.MemoryCopy(src, result.Data, 4096, value.Length * sizeof(char));
                result.Data[value.Length] = '\0';
            }

            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvDrsProfile
    {
        public uint Version;
        public NvApiUnicodeString ProfileName;
        public uint GpuSupport;
        public uint IsPredefined;
        public uint NumOfApps;
        public uint NumOfSettings;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvDrsApplication
    {
        public uint Version;
        public uint IsPredefined;
        public NvApiUnicodeString AppName;
        public NvApiUnicodeString UserFriendlyName;
        public NvApiUnicodeString Launcher;
        public NvApiUnicodeString FileInFolder;
        public uint Flags;
        public NvApiUnicodeString CommandLine;
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
    }



    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint NvapiQueryInterface(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiInitialize();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiUnload();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsCreateSession(out nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsDestroySession(nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsLoadSettings(nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsSaveSettings(nint session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsFindProfileByName(nint session, NvApiUnicodeString profileName, out nint profile);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsCreateProfile(nint session, NvDrsProfile* profileInfo, out nint profile);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsFindApplicationByName(nint session, NvApiUnicodeString appName, out nint profile, NvDrsApplication* application);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsCreateApplication(nint session, nint profile, NvDrsApplication* application);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsSetSetting(nint session, nint profile, NvDrsSetting* setting);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsGetSetting(nint session, nint profile, uint settingId, NvDrsSetting* setting);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvapiDrsRestoreAllDefaults(nint session);
}