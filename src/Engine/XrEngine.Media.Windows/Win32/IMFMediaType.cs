using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Xml.Serialization;

namespace XrEngine.Media.Windows
{
    [GeneratedComInterface, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMFMediaType : IMFAttributes
    {
        // HRESULT GetMajorType(GUID *pguidMajorType)
        int GetMajorType(out Guid pguidMajorType);

        // HRESULT IsCompressedFormat(BOOL *pfCompressed)
        int IsCompressedFormat([MarshalAs(UnmanagedType.Bool)] out bool pfCompressed);

        // HRESULT IsEqual(IMFMediaType *pIMediaType, DWORD *pdwFlags)
        int IsEqual(IMFMediaType pIMediaType, out int pdwFlags);

        // HRESULT GetRepresentation(GUID guidRepresentation, LPVOID *ppvRepresentation)
        int GetRepresentation(ref Guid guidRepresentation, out IntPtr ppvRepresentation);

        // HRESULT FreeRepresentation(GUID guidRepresentation, LPVOID pvRepresentation)
        int FreeRepresentation(ref Guid guidRepresentation, IntPtr pvRepresentation);
    }
}
