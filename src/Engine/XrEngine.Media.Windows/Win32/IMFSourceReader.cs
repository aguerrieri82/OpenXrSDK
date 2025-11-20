using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace XrEngine.Media.Windows
{
    [GeneratedComInterface]
    [Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMFSourceReader
    {
        // HRESULT GetStreamSelection(DWORD, BOOL*)
        int GetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] out bool pfSelected);

        // HRESULT SetStreamSelection(DWORD, BOOL)
        int SetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] bool fSelected);

        // HRESULT GetNativeMediaType(DWORD, DWORD, IMFMediaType**)
        int GetNativeMediaType(int dwStreamIndex, int dwMediaTypeIndex,
                               out IMFMediaType ppMediaType);

        // HRESULT GetCurrentMediaType(DWORD, IMFMediaType**)
        int GetCurrentMediaType(int dwStreamIndex,
                                out IMFMediaType ppMediaType);

        // HRESULT SetCurrentMediaType(DWORD, DWORD*, IMFMediaType*)
        int SetCurrentMediaType(int dwStreamIndex, IntPtr pdwReserved,
                                IMFMediaType pMediaType);

        // HRESULT SetCurrentPosition(REFGUID, REFPROPVARIANT)
        int SetCurrentPosition(ref Guid guidTimeFormat, ref PropVariant varPosition);

        // HRESULT ReadSample(DWORD, DWORD, DWORD*, DWORD*, LONGLONG*, IMFSample**)
        int ReadSample(int dwStreamIndex, int dwControlFlags,
                       out int pdwActualStreamIndex,
                       out int pdwStreamFlags,
                       out long pllTimestamp,
                       out IMFSample ppSample);

        // HRESULT Flush(DWORD)
        int Flush(int dwStreamIndex);

        // HRESULT GetServiceForStream(DWORD, REFGUID, REFIID, LPVOID*)
        int GetServiceForStream(int dwStreamIndex,
                                ref Guid guidService,
                                ref Guid riid,
                                out IntPtr ppvObject);

        // HRESULT GetPresentationAttribute(DWORD, REFGUID, PROPVARIANT*)
        int GetPresentationAttribute(int dwStreamIndex,
                                     ref Guid guidAttribute,
                                     out PropVariant pvarAttribute);
    }
}
