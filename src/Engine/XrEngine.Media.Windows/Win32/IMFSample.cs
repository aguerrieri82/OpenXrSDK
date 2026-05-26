using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace XrEngine.Media.Windows
{
    [GeneratedComInterface]
    [Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMFSample
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


        // HRESULT GetSampleFlags(DWORD *pdwSampleFlags)
        [PreserveSig]
        int GetSampleFlags(out uint pdwSampleFlags);

        // HRESULT SetSampleFlags(DWORD dwSampleFlags)
        [PreserveSig]
        int SetSampleFlags(uint dwSampleFlags);

        // HRESULT GetSampleTime(LONGLONG *phnsSampleTime)
        [PreserveSig]
        int GetSampleTime(out long phnsSampleTime);

        // HRESULT SetSampleTime(LONGLONG hnsSampleTime)
        [PreserveSig]
        int SetSampleTime(long hnsSampleTime);

        // HRESULT GetSampleDuration(LONGLONG *phnsSampleDuration)
        [PreserveSig]
        int GetSampleDuration(out long phnsSampleDuration);

        // HRESULT SetSampleDuration(LONGLONG hnsSampleDuration)
        [PreserveSig]
        int SetSampleDuration(long hnsSampleDuration);

        // HRESULT GetBufferCount(DWORD *pdwBufferCount)
        [PreserveSig]
        int GetBufferCount(out uint pdwBufferCount);

        // HRESULT GetBufferByIndex(DWORD dwIndex, IMFMediaBuffer **ppBuffer)
        [PreserveSig]
        int GetBufferByIndex(
            uint dwIndex,
            out IMFMediaBuffer ppBuffer);

        // HRESULT ConvertToContiguousBuffer(IMFMediaBuffer **ppBuffer)
        [PreserveSig]
        int ConvertToContiguousBuffer(
            out IMFMediaBuffer ppBuffer);

        // HRESULT AddBuffer(IMFMediaBuffer *pBuffer)
        [PreserveSig]
        int AddBuffer(IMFMediaBuffer pBuffer);

        // HRESULT RemoveBufferByIndex(DWORD dwIndex)
        [PreserveSig]
        int RemoveBufferByIndex(uint dwIndex);

        // HRESULT RemoveAllBuffers(void)
        [PreserveSig]
        int RemoveAllBuffers();

        // HRESULT GetTotalLength(DWORD *pcbTotalLength)
        [PreserveSig]
        int GetTotalLength(out uint pcbTotalLength);

        // HRESULT CopyToBuffer(IMFMediaBuffer *pBuffer)
        [PreserveSig]
        int CopyToBuffer(IMFMediaBuffer pBuffer);
    }
}
