using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework.Oculus.Structs
{
    public class METAVirtualKeyboard : BaseXrExtension
    {
        public METAVirtualKeyboard(XR xr, Instance instance)
            : base(xr, instance) 
        {
            
        }

        public CreateVirtualKeyboardMETADelegate? CreateVirtualKeyboardMETA;
        public DestroyVirtualKeyboardMETADelegate? DestroyVirtualKeyboardMETA;
        public CreateVirtualKeyboardSpaceMETADelegate? CreateVirtualKeyboardSpaceMETA;
        public SuggestVirtualKeyboardLocationMETADelegate? SuggestVirtualKeyboardLocationMETA;
        public GetVirtualKeyboardScaleMETADelegate? GetVirtualKeyboardScaleMETA;
        public SetVirtualKeyboardModelVisibilityMETADelegate? SetVirtualKeyboardModelVisibilityMETA;
        public GetVirtualKeyboardModelAnimationStatesMETADelegate? GetVirtualKeyboardModelAnimationStatesMETA;
        public GetVirtualKeyboardDirtyTexturesMETADelegate? GetVirtualKeyboardDirtyTexturesMETA;
        public GetVirtualKeyboardTextureDataMETADelegate? GetVirtualKeyboardTextureDataMETA;
        public SendVirtualKeyboardInputMETADelegate? SendVirtualKeyboardInputMETA;
        public ChangeVirtualKeyboardTextContextMETADelegate? ChangeVirtualKeyboardTextContextMETA;


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateVirtualKeyboardMETADelegate(Session session, ref VirtualKeyboardCreateInfoMETA createInfo, ref VirtualKeyboardMETA keyboard);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyVirtualKeyboardMETADelegate(VirtualKeyboardMETA keyboard);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateVirtualKeyboardSpaceMETADelegate(Session session, VirtualKeyboardMETA keyboard, ref VirtualKeyboardSpaceCreateInfoMETA createInfo, ref Space keyboardSpace);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SuggestVirtualKeyboardLocationMETADelegate(VirtualKeyboardMETA keyboard, ref VirtualKeyboardLocationInfoMETA locationInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetVirtualKeyboardScaleMETADelegate(VirtualKeyboardMETA keyboard, ref float scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SetVirtualKeyboardModelVisibilityMETADelegate(VirtualKeyboardMETA keyboard, ref VirtualKeyboardModelVisibilitySetInfoMETA modelVisibility);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetVirtualKeyboardModelAnimationStatesMETADelegate(VirtualKeyboardMETA keyboard, ref VirtualKeyboardModelAnimationStatesMETA animationStates);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe public delegate Result GetVirtualKeyboardDirtyTexturesMETADelegate(VirtualKeyboardMETA keyboard, uint textureIdCapacityInput, ref uint textureIdCountOutput, ulong* textureIds);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetVirtualKeyboardTextureDataMETADelegate(VirtualKeyboardMETA keyboard, ulong textureId, ref VirtualKeyboardTextureDataMETA textureData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SendVirtualKeyboardInputMETADelegate(VirtualKeyboardMETA keyboard, ref VirtualKeyboardInputInfoMETA info, ref Posef interactorRootPose);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result ChangeVirtualKeyboardTextContextMETADelegate(VirtualKeyboardMETA keyboard, ref VirtualKeyboardTextContextChangeInfoMETA changeInfo);


        public const string ExtensionName = "XR_META_virtual_keyboard";
    }
}
