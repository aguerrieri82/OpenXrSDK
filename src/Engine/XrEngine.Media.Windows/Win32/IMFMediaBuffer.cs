using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace XrEngine.Media.Windows
{
    [GeneratedComInterface]
    [Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMFMediaBuffer
    {
        // HRESULT Lock(BYTE **ppbBuffer, DWORD *pcbMaxLength, DWORD *pcbCurrentLength)
        int Lock(
            out IntPtr ppbBuffer,
            out int pcbMaxLength,
            out int pcbCurrentLength
        );

        // HRESULT Unlock()
        int Unlock();

        // HRESULT GetCurrentLength(DWORD *pcbCurrentLength)
        int GetCurrentLength(out int pcbCurrentLength);

        // HRESULT SetCurrentLength(DWORD cbCurrentLength)
        int SetCurrentLength(int cbCurrentLength);

        // HRESULT GetMaxLength(DWORD *pcbMaxLength)
        int GetMaxLength(out int pcbMaxLength);
    }
}
