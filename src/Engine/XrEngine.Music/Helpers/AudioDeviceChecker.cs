#if !__ANDROID__

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Music
{
    public static class AudioBluetoothDetector
    {
        private static readonly Guid CLSID_MMDeviceEnumerator =
            new("BCDE0395-E52F-467C-8E3D-C4579291692E");

        private static readonly Guid GUID_BUS_TYPE_BLUETOOTH =
            new("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");



        // --- DEVPROPKEY per device PnP (SetupAPI, devpkey.h) ---

        private static DEVPROPKEY DEVPKEY_Device_FriendlyName =
            new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

        private static DEVPROPKEY DEVPKEY_Device_BusTypeGuid =
            new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 21);  // GUID

        private static DEVPROPKEY DEVPKEY_Device_EnumeratorName =
            new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 24);  // string

        private static DEVPROPKEY PKEY_Device_FriendlyName =
            new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

        private static DEVPROPKEY PKEY_Device_ContainerId =
            new(new Guid("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"), 2);

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_ALLCLASSES = 0x00000004;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        public static string? GetDefaultOutputDeviceName()
        {
            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(
                Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator)!)!;

            enumerator.GetDefaultAudioEndpoint(
                EDataFlow.eRender,
                ERole.eMultimedia,
                out var endpoint);

            endpoint.OpenPropertyStore(0, out var store);

            return TryGetString(store, PKEY_Device_FriendlyName, out var name)
                ? name
                : null;
        }


        // =========================================================
        // API pubblica
        // =========================================================

        public static bool IsDefaultOutputDeviceBluetooth()
        {
            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(
                Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator)!)!;

            enumerator.GetDefaultAudioEndpoint(
                EDataFlow.eRender,
                ERole.eMultimedia,
                out var endpoint);

            endpoint.OpenPropertyStore(0, out var store);

            if (!TryGetGuid(store, PKEY_Device_ContainerId, out var contId))
                return false;

            return IsBluetoothFromPnP(contId);
        }

        // =========================================================
        // Detection Bluetooth via SetupAPI + walk parent
        // =========================================================

        private static bool TryGetString(IPropertyStore store, DEVPROPKEY key, out string? value)
        {
            value = null;

            store.GetValue(ref key, out var pv);

            try
            {
                // VT_LPWSTR
                if (pv.vt != 31 || pv.pointerValue == IntPtr.Zero)
                    return false;

                value = Marshal.PtrToStringUni(pv.pointerValue);
                return !string.IsNullOrEmpty(value);
            }
            finally
            {
                if (pv.vt != 0)
                    PropVariantClear(ref pv);
            }
        }

        private static bool IsBluetoothFromPnP(Guid contId)
        {
            var devInfoSet = SetupDiGetClassDevs(
                IntPtr.Zero,
                null,
                IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_ALLCLASSES);

            if (devInfoSet == IntPtr.Zero || devInfoSet == new IntPtr(-1))
                return false;

            try
            {
                var devInfoData = new SP_DEVINFO_DATA
                {
                    cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>()
                };

                uint index = 0;
                while (SetupDiEnumDeviceInfo(devInfoSet, index++, ref devInfoData))
                {
                    // Contenitore PnP (devnode che implementa l’endpoint)
                    if (!TryGetDeviceGuidProperty(
                            devInfoSet, ref devInfoData,
                            PKEY_Device_ContainerId,
                            out var curContId))
                        continue;

                    if (curContId != contId)
                        continue;

                    // Debug opzionale: friendly name del devnode di partenza
                    if (TryGetDeviceStringProperty(
                            devInfoSet, ref devInfoData,
                            DEVPKEY_Device_FriendlyName,
                            out var name))
                    {
                        Debug.WriteLine($"Start devnode: {name}");
                    }

                    // Da questo devInst risali i parent e cerca BTHENUM ecc.
                    return IsBluetoothFromDevInst(devInfoData.DevInst);
                }

                return false;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devInfoSet);
            }
        }

        private static bool IsBluetoothFromDevInst(uint devInst)
        {
            var current = devInst;

            while (true)
            {
                // 1) BusTypeGuid
                if (TryGetDeviceGuidPropertyByDevInst(
                        current,
                        DEVPKEY_Device_BusTypeGuid,
                        out var busGuid))
                {
                    if (busGuid == GUID_BUS_TYPE_BLUETOOTH)
                        return true;
                }

                // 2) EnumeratorName = BTHENUM
                if (TryGetDeviceStringPropertyByDevInst(
                        current,
                        DEVPKEY_Device_EnumeratorName,
                        out var enumName))
                {
                    if (enumName != null && enumName.Equals("BTHENUM", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Risali al parent
                var cr = CM_Get_Parent(out var parent, current, 0);
                if (cr != 0)
                    break;

                current = parent;
            }

            return false;
        }

        // =========================================================
        // Lettura proprietà da IPropertyStore (endpoint audio)
        // =========================================================

        private static bool TryGetGuid(IPropertyStore store, DEVPROPKEY key, out Guid value)
        {
            value = Guid.Empty;
            store.GetValue(ref key, out var pv);
            try
            {
                // VT_CLSID = 72
                if (pv.vt != 72 || pv.pointerValue == IntPtr.Zero)
                    return false;

                value = Marshal.PtrToStructure<Guid>(pv.pointerValue);
                return true;
            }
            finally
            {
                if (pv.vt != 0)
                    PropVariantClear(ref pv);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);

        // =========================================================
        // Lettura proprietà da SetupAPI (con DeviceInfoSet + SP_DEVINFO_DATA)
        // =========================================================

        private static bool TryGetDeviceGuidProperty(
            IntPtr devInfoSet,
            ref SP_DEVINFO_DATA devInfoData,
            DEVPROPKEY key,
            out Guid value)
        {
            value = Guid.Empty;

            uint propType;
            uint requiredSize;

            if (!SetupDiGetDeviceProperty(
                    devInfoSet, ref devInfoData,
                    ref key, out propType,
                    null, 0, out requiredSize, 0))
            {
                var err = Marshal.GetLastWin32Error();
                if (err != ERROR_INSUFFICIENT_BUFFER)
                    return false;
            }

            var buffer = new byte[requiredSize];

            if (!SetupDiGetDeviceProperty(
                    devInfoSet, ref devInfoData,
                    ref key, out propType,
                    buffer, (uint)buffer.Length,
                    out requiredSize, 0))
            {
                return false;
            }

            if (buffer.Length < 16)
                return false;

            value = new Guid(new ReadOnlySpan<byte>(buffer, 0, 16));
            return true;
        }

        private static bool TryGetDeviceStringProperty(
            IntPtr devInfoSet,
            ref SP_DEVINFO_DATA devInfoData,
            DEVPROPKEY key,
            out string? value)
        {
            value = null;

            uint propType;
            uint requiredSize;

            if (!SetupDiGetDeviceProperty(
                    devInfoSet, ref devInfoData,
                    ref key, out propType,
                    null, 0, out requiredSize, 0))
            {
                var err = Marshal.GetLastWin32Error();
                if (err != ERROR_INSUFFICIENT_BUFFER)
                    return false;
            }

            var buffer = new byte[requiredSize];

            if (!SetupDiGetDeviceProperty(
                    devInfoSet, ref devInfoData,
                    ref key, out propType,
                    buffer, (uint)buffer.Length,
                    out requiredSize, 0))
            {
                return false;
            }

            var s = Encoding.Unicode.GetString(buffer);
            var nul = s.IndexOf('\0');
            if (nul >= 0)
                s = s[..nul];

            value = s;
            return !string.IsNullOrEmpty(value);
        }

        // =========================================================
        // Helper: lettura proprietà per DEVINST (cammina via InstanceId)
        // =========================================================

        private static bool TryGetDeviceStringPropertyByDevInst(
            uint devInst,
            DEVPROPKEY key,
            out string? value)
        {
            value = null;

            var instanceId = GetInstanceIdFromDevInst(devInst);
            if (instanceId == null)
                return false;

            var set = SetupDiGetClassDevs(
                IntPtr.Zero, null, IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_ALLCLASSES);

            if (set == IntPtr.Zero || set == new IntPtr(-1))
                return false;

            try
            {
                var data = new SP_DEVINFO_DATA
                {
                    cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>()
                };

                if (!SetupDiOpenDeviceInfo(set, instanceId, IntPtr.Zero, 0, ref data))
                    return false;

                return TryGetDeviceStringProperty(set, ref data, key, out value);
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(set);
            }
        }

        private static bool TryGetDeviceGuidPropertyByDevInst(
            uint devInst,
            DEVPROPKEY key,
            out Guid value)
        {
            value = Guid.Empty;

            var instanceId = GetInstanceIdFromDevInst(devInst);
            if (instanceId == null)
                return false;

            var set = SetupDiGetClassDevs(
                IntPtr.Zero, null, IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_ALLCLASSES);

            if (set == IntPtr.Zero || set == new IntPtr(-1))
                return false;

            try
            {
                var data = new SP_DEVINFO_DATA
                {
                    cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>()
                };

                if (!SetupDiOpenDeviceInfo(set, instanceId, IntPtr.Zero, 0, ref data))
                    return false;

                return TryGetDeviceGuidProperty(set, ref data, key, out value);
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(set);
            }
        }

        private static string? GetInstanceIdFromDevInst(uint devInst)
        {
            var sb = new StringBuilder(512);
            var cr = CM_Get_Device_ID(devInst, sb, sb.Capacity, 0);
            if (cr != 0)
                return null;
            return sb.ToString();
        }

        // =========================================================
        // SetupAPI + cfgmgr interop
        // =========================================================

        [DllImport("Setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            string? Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet);

        [DllImport("Setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            byte[]? PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize,
            uint Flags);

        [DllImport("Setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiOpenDeviceInfo(
            IntPtr DeviceInfoSet,
            string DeviceInstanceId,
            IntPtr hwndParent,
            uint OpenFlags,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("cfgmgr32.dll")]
        private static extern int CM_Get_Parent(
            out uint pdnDevInst,
            uint dnDevInst,
            int ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern int CM_Get_Device_ID(
            uint dnDevInst,
            StringBuilder Buffer,
            int BufferLen,
            int ulFlags);
    }

    // =========================================================
    // Strutture & COM interop
    // =========================================================

    enum EDataFlow
    {
        eRender,
        eCapture,
        eAll
    }

    enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;

        public DEVPROPKEY(Guid f, uint p)
        {
            fmtid = f;
            pid = p;
        }

        public DEVPROPKEY(Guid f, int p)
        {
            fmtid = f;
            pid = (uint)p;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]

    struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pointerValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        int GetCount(out int cProps);
        int GetAt(int iProp, out DEVPROPKEY pkey);
        int GetValue(ref DEVPROPKEY key, out PROPVARIANT pv);
        int SetValue(ref DEVPROPKEY key, ref PROPVARIANT propvar);
        int Commit();
    }
}

#endif