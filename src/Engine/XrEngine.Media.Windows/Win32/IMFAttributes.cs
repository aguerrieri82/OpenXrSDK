using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace XrEngine.Media.Windows
{

    [GeneratedComInterface]
    [Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMFAttributes
    {
        int GetItem(ref Guid guidKey, nint pValue); // PROPVARIANT*

        int GetItemType(ref Guid guidKey, out uint pType);

        int CompareItem(ref Guid guidKey, nint value /* PROPVARIANT*/ , out int pbResult);

        int Compare(IMFAttributes theirs, uint matchType, out int pbResult);

        int GetUINT32(ref Guid guidKey, out uint value);

        int GetUINT64(ref Guid guidKey, out ulong value);

        int GetDouble(ref Guid guidKey, out double value);

        int GetGUID(ref Guid guidKey, out Guid value);

        int GetStringLength(ref Guid guidKey, out uint pcchLength);

        int GetString(ref Guid guidKey, nint pwszValue /* wchar_t* */, uint cchBufSize, ref uint pcchLength);

        int GetAllocatedString(ref Guid guidKey, out nint ppwszValue, out uint pcchLength);

        int GetBlobSize(ref Guid guidKey, out uint pcbBlobSize);

        int GetBlob(ref Guid guidKey, nint pBuf /* BYTE* */, uint cbBufSize, ref uint pcbBlobSize);

        int GetAllocatedBlob(ref Guid guidKey, out nint ppBuf, out uint pcbSize);

        int GetUnknown(ref Guid guidKey, ref Guid riid, out nint ppv);

        int SetItem(ref Guid guidKey, nint value /* PROPVARIANT*/);

        int DeleteItem(ref Guid guidKey);

        int DeleteAllItems();

        int SetUINT32(ref Guid guidKey, uint value);

        int SetUINT64(ref Guid guidKey, ulong value);

        int SetDouble(ref Guid guidKey, double value);

        int SetGUID(ref Guid guidKey, ref Guid value);

        int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string value);

        int SetBlob(ref Guid guidKey, nint pBuf /* BYTE* */, uint cbBufSize);

        int SetUnknown(ref Guid guidKey, nint pUnknown /* IUnknown* */);

        int LockStore();

        int UnlockStore();

        int GetCount(out uint pcItems);

        int GetItemByIndex(uint index, out Guid guid, nint pValue /* PROPVARIANT*/);

        int CopyAllItems(IMFAttributes dest);
    }
}
