using OpenXr.Framework.Oculus.Structs;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public interface IXrVirtualKeyboardEventDispatcher
    {
        void OnCommitText(string text);

        void OnBackspace();

        void OnEnter();

        void OnShown();

        void OnHidden();
    }

    public class XrVirtualKeyboard : IDisposable
    {
        readonly XrApp _app;
        FBRenderModel? _modelExt;
        METAVirtualKeyboard? _keyExt;
        VirtualKeyboardMETA _keyboard;
        Space _keyboardSpace;
        private XrPoseInput? _leftAim;
        private XrPoseInput? _rightAim;
        private XrBoolInput? _leftTrigger;
        private XrBoolInput? _rightTrigger;

        public XrVirtualKeyboard(XrApp app)
        {
            _app = app;
        }

        protected void Initialize()
        {
            _keyExt = new METAVirtualKeyboard(_app.Xr, _app.Instance);
            _app.Xr.TryGetInstanceExtension<FBRenderModel>(null, _app.Instance, out _modelExt);

            _app.Inputs.TryGetValue("LeftAimPose", out var leftAim);
            _app.Inputs.TryGetValue("RightAimPose", out var rightAim);
            _app.Inputs.TryGetValue("LeftTriggerClick", out var leftTrigger);
            _app.Inputs.TryGetValue("RightTriggerClick", out var rightTrigger);

            _leftAim = leftAim as XrPoseInput;
            _rightAim = rightAim as XrPoseInput;
            _leftTrigger = leftTrigger as XrBoolInput;
            _rightTrigger = rightTrigger as XrBoolInput;

            _app.XrEvent += OnEvent;
        }

        unsafe void OnEvent(ref EventDataBuffer buffer)
        {
            switch (buffer.Type)
            {
                case StructureType.EventDataVirtualKeyboardCommitTextMeta:
                    {
                        fixed (EventDataBuffer* pBuffer = &buffer)
                        {
                            var evt = (EventDataVirtualKeyboardCommitTextMETA*)pBuffer;
                            var text = Marshal.PtrToStringUTF8((nint)evt->Text);

                            if (text != null)
                                EventDispatcher?.OnCommitText(text);
                        }
                        break;
                    }

                case StructureType.EventDataVirtualKeyboardBackspaceMeta:
                    EventDispatcher?.OnBackspace();
                    break;

                case StructureType.EventDataVirtualKeyboardEnterMeta:
                    EventDispatcher?.OnEnter();
                    break;

                case StructureType.EventDataVirtualKeyboardShownMeta:
                    EventDispatcher?.OnShown();
                    break;

                case StructureType.EventDataVirtualKeyboardHiddenMeta:
                    EventDispatcher?.OnHidden();
                    break;
            }
        }

        public unsafe void SetTextContext(string text)
        {
            var utf8 = Encoding.UTF8.GetBytes(text + '\0');

            fixed (byte* pText = utf8)
            {
                var info = new VirtualKeyboardTextContextChangeInfoMETA
                {
                    Type = StructureType.VirtualKeyboardTextContextChangeInfoMeta,
                    TextContext = pText
                };

                _app.CheckResult(_keyExt!.ChangeVirtualKeyboardTextContextMETA!(_keyboard, ref info), "ChangeVirtualKeyboardTextContextMETA");
            }
        }

        public bool IsSupported()
        {
            Debug.WriteLine($"XR_META_virtual_keyboard: {_app.Xr.IsInstanceExtensionPresent(null, "XR_META_virtual_keyboard")}");
            Debug.WriteLine($"XR_FB_render_model: {_app.Xr.IsInstanceExtensionPresent(null, "XR_FB_render_model")}");

            var bodyProps = new SystemVirtualKeyboardPropertiesMETA
            {
                Type = StructureType.SystemVirtualKeyboardPropertiesMeta
            };

            _app.GetSystemProperties(ref bodyProps);

            return bodyProps.SupportsVirtualKeyboard != 0;
        }

        public void Create()
        {
            Create(VirtualKeyboardLocationTypeMETA.DirectMeta, null);
        }

        public void Create(Pose3 pose)
        {
            Create(VirtualKeyboardLocationTypeMETA.CustomMeta, pose);
        }

        public void Create(bool isFar)
        {
            Create(isFar ? VirtualKeyboardLocationTypeMETA.FarMeta : VirtualKeyboardLocationTypeMETA.DirectMeta, null);
        }

        protected void Create(VirtualKeyboardLocationTypeMETA locType, Pose3? pose)
        {
            Initialize();

            var info = new VirtualKeyboardCreateInfoMETA()
            {
                Type = StructureType.VirtualKeyboardCreateInfoMeta
            };

            _app.CheckResult(_keyExt!.CreateVirtualKeyboardMETA!(_app.Session, ref info, ref _keyboard), "CreateVirtualKeyboardMETA");

            var space = new VirtualKeyboardSpaceCreateInfoMETA()
            {
                Type = StructureType.VirtualKeyboardSpaceCreateInfoMeta,
                Space = _app.ReferenceSpace,
            };

            if (pose != null)
            {
                space.PoseInSpace = pose.Value.ToPoseF();
                space.LocationType = VirtualKeyboardLocationTypeMETA.CustomMeta;
            }
            else
                space.LocationType = locType;

            _app.CheckResult(_keyExt!.CreateVirtualKeyboardSpaceMETA!(_app.Session, _keyboard, ref space, ref _keyboardSpace), "CreateVirtualKeyboardSpaceMETA");
        }

        public MemoryStream LoadModel()
        {
            var key = GetVirtualKeyboardModelKey();
            return LoadVirtualKeyboardModel(key);
        }

        public void SetVisible(bool visible)
        {
            var info = new VirtualKeyboardModelVisibilitySetInfoMETA
            {
                Type = StructureType.VirtualKeyboardModelVisibilitySetInfoMeta,
                Visible = visible ? 1u : 0u
            };

            _app.CheckResult(_keyExt!.SetVirtualKeyboardModelVisibilityMETA!(_keyboard, ref info), "SetVirtualKeyboardModelVisibilityMETA");
        }

        public (Pose3 Pose, float Scale) GetLocation()
        {
            var location = _app.LocateSpace(_keyboardSpace, _app.ReferenceSpace);

            float scale = 1;

            _app.CheckResult(_keyExt!.GetVirtualKeyboardScaleMETA!(_keyboard, ref scale), "GetVirtualKeyboardScaleMETA");

            return (location.Pose, scale);
        }

        public void SetLocation(VirtualKeyboardLocationTypeMETA type)
        {
            var info = new VirtualKeyboardLocationInfoMETA
            {
                Type = StructureType.VirtualKeyboardLocationInfoMeta,
                LocationType = type,
                Space = _app.ReferenceSpace
            };

            _app.CheckResult(_keyExt!.SuggestVirtualKeyboardLocationMETA!(_keyboard, ref info), "SuggestVirtualKeyboardLocationMETA");
        }

        public void SetLocation(Pose3 pose, float scale = 1)
        {
            var info = new VirtualKeyboardLocationInfoMETA
            {
                Type = StructureType.VirtualKeyboardLocationInfoMeta,
                LocationType = VirtualKeyboardLocationTypeMETA.CustomMeta,
                Space = _app.ReferenceSpace,
                PoseInSpace = pose.ToPoseF(),
                Scale = scale
            };

            _app.CheckResult(_keyExt!.SuggestVirtualKeyboardLocationMETA!(_keyboard, ref info), "SuggestVirtualKeyboardLocationMETA");
        }

        public void UpdateInputs()
        {
            if (_leftAim != null)
                SendControllerInput(_leftAim, _leftTrigger, VirtualKeyboardInputSourceMETA.ControllerRayLeftMeta);

            if (_rightAim != null)
                SendControllerInput(_rightAim, _rightTrigger, VirtualKeyboardInputSourceMETA.ControllerRayRightMeta);

            if (_app.Hands.TryGetValue(HandEXT.LeftExt, out var leftHand))
                SendHandInput(leftHand, VirtualKeyboardInputSourceMETA.HandDirectIndexTipLeftMeta);

            if (_app.Hands.TryGetValue(HandEXT.RightExt, out var rightHand))
                SendHandInput(rightHand, VirtualKeyboardInputSourceMETA.HandDirectIndexTipRightMeta);
        }

        public unsafe VirtualKeyboardAnimationStateMETA[] GetAnimationStates()
        {
            var states = new VirtualKeyboardModelAnimationStatesMETA
            {
                Type = StructureType.VirtualKeyboardModelAnimationStatesMeta
            };

            _app.CheckResult(_keyExt!.GetVirtualKeyboardModelAnimationStatesMETA!(_keyboard, ref states), "GetVirtualKeyboardModelAnimationStatesMETA");

            if (states.StateCountOutput == 0)
                return [];

            var result = new VirtualKeyboardAnimationStateMETA[states.StateCountOutput];

            for (var i = 0; i < result.Length; i++)
                result[i].Type = StructureType.VirtualKeyboardAnimationStateMeta;

            fixed (VirtualKeyboardAnimationStateMETA* pStates = result)
            {
                states.StateCapacityInput = states.StateCountOutput;
                states.States = pStates;

                _app.CheckResult(_keyExt.GetVirtualKeyboardModelAnimationStatesMETA!(_keyboard, ref states), "GetVirtualKeyboardModelAnimationStatesMETA");
            }

            if (states.StateCountOutput != result.Length)
                Array.Resize(ref result, (int)states.StateCountOutput);

            return result;
        }

        public unsafe Dictionary<ulong, byte[]> GetDirtyTextures()
        {
            uint textureCount = 0;

            _app.CheckResult(_keyExt!.GetVirtualKeyboardDirtyTexturesMETA!(_keyboard, 0, ref textureCount, null), "GetVirtualKeyboardDirtyTexturesMETA");

            var result = new Dictionary<ulong, byte[]>((int)textureCount);

            if (textureCount == 0)
                return result;

            var textureIds = new ulong[textureCount];

            fixed (ulong* pTextureIds = textureIds)
            {
                _app.CheckResult(_keyExt.GetVirtualKeyboardDirtyTexturesMETA!(_keyboard, textureCount, ref textureCount, pTextureIds), "GetVirtualKeyboardDirtyTexturesMETA");
            }

            foreach (var textureId in textureIds)
            {
                var textureData = new VirtualKeyboardTextureDataMETA
                {
                    Type = StructureType.VirtualKeyboardTextureDataMeta
                };

                _app.CheckResult(_keyExt.GetVirtualKeyboardTextureDataMETA!(_keyboard, textureId, ref textureData), "GetVirtualKeyboardTextureDataMETA");

                var buffer = new byte[textureData.BufferCountOutput];

                fixed (byte* pBuffer = buffer)
                {
                    textureData.BufferCapacityInput = textureData.BufferCountOutput;
                    textureData.Buffer = pBuffer;

                    _app.CheckResult(_keyExt.GetVirtualKeyboardTextureDataMETA!(_keyboard, textureId, ref textureData), "GetVirtualKeyboardTextureDataMETA");
                }

                result[textureId] = buffer;
            }

            return result;
        }

        public bool TryParseTextureUri(string url, out ulong textureId, out int width, out int height)
        {
            textureId = 0;
            width = 0;
            height = 0;

            const string prefix = "metaVirtualKeyboard://texture/";

            if (!url.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var queryIndex = url.IndexOf('?', prefix.Length);

            if (queryIndex < 0)
                return false;

            if (!ulong.TryParse(url.AsSpan(prefix.Length, queryIndex - prefix.Length), out textureId))
                return false;

            var query = url.AsSpan(queryIndex + 1);

            var widthEnd = query.IndexOf('&');

            if (widthEnd < 0 || !query[..widthEnd].StartsWith("w=") ||
                !int.TryParse(query[2..widthEnd], out width))
                return false;

            query = query[(widthEnd + 1)..];

            var heightEnd = query.IndexOf('&');

            if (heightEnd < 0 || !query[..heightEnd].StartsWith("h=") ||
                !int.TryParse(query[2..heightEnd], out height))
                return false;

            query = query[(heightEnd + 1)..];

            if (!query.SequenceEqual("fmt=RGBA32"))
                return false;

            return width > 0 && height > 0;
        }
        protected void SendHandInput(XrHandInput hand, VirtualKeyboardInputSourceMETA source)
        {
            if (!hand.IsActive || hand.Joints == null)
                return;

            var joint = hand.Joints[(int)HandJointEXT.IndexTipExt];

            if ((joint.LocationFlags & SpaceLocationFlags.PositionValidBit) == 0)
                return;

            var info = new VirtualKeyboardInputInfoMETA
            {
                Type = StructureType.VirtualKeyboardInputInfoMeta,
                InputSource = source,
                InputSpace = _app.ReferenceSpace,
                InputPoseInSpace = joint.Pose
            };

            var rootPose = new Posef();

            _app.CheckResult(_keyExt!.SendVirtualKeyboardInputMETA!(_keyboard, ref info, ref rootPose), "SendVirtualKeyboardInputMETA");
        }

        protected void SendControllerInput(XrPoseInput? pose, XrBoolInput? trigger, VirtualKeyboardInputSourceMETA source)
        {
            if (pose == null || !pose.IsActive)
                return;

            var info = new VirtualKeyboardInputInfoMETA
            {
                Type = StructureType.VirtualKeyboardInputInfoMeta,
                InputSource = source,
                InputSpace = _app.ReferenceSpace,
                InputPoseInSpace = pose.Value.ToPoseF(),
                InputState = trigger?.IsActive == true && trigger.Value
                    ? VirtualKeyboardInputStateFlagsMETA.PressedBitMeta
                    : 0
            };

            var rootPose = new Posef();

            _app.CheckResult(_keyExt!.SendVirtualKeyboardInputMETA!(_keyboard, ref info, ref rootPose), "SendVirtualKeyboardInputMETA");
        }

        unsafe ulong GetVirtualKeyboardModelKey()
        {
            const string keyboardPath = "/model_meta/keyboard/virtual";

            uint pathCount = 0;
            _app.CheckResult(_modelExt!.EnumerateRenderModelPathsFB(_app.Session, 0, &pathCount, null), "EnumerateRenderModelPathsFB");

            var pathInfos = new RenderModelPathInfoFB[pathCount];

            for (var i = 0; i < pathInfos.Length; i++)
                pathInfos[i].Type = StructureType.RenderModelPathInfoFB;

            fixed (RenderModelPathInfoFB* pPathInfos = pathInfos)
            {
                _app.CheckResult(_modelExt.EnumerateRenderModelPathsFB(_app.Session, pathCount, &pathCount, pPathInfos), "EnumerateRenderModelPathsFB");
            }

            foreach (var info in pathInfos)
            {
                var pathString = _app.PathToString(info.Path);

                var capReq = new RenderModelCapabilitiesRequestFB
                {
                    Type = StructureType.RenderModelCapabilitiesRequestFB,
                    Flags = RenderModelFlagsFB.Subset2BitFB
                };

                var properties = new RenderModelPropertiesFB
                {
                    Type = StructureType.RenderModelPropertiesFB,
                    Next = &capReq
                };

                _modelExt.GetRenderModelPropertiesFB(_app.Session, info.Path, ref properties);

                if (pathString == keyboardPath)
                    return properties.ModelKey;
            }

            return 0;
        }

        unsafe MemoryStream LoadVirtualKeyboardModel(ulong modelKey)
        {
            var loadInfo = new RenderModelLoadInfoFB
            {
                Type = StructureType.RenderModelLoadInfoFB,
                ModelKey = modelKey
            };

            var renderModelBuffer = new RenderModelBufferFB
            {
                Type = StructureType.RenderModelBufferFB
            };

            _app.CheckResult(_modelExt!.LoadRenderModelFB(_app.Session, &loadInfo, ref renderModelBuffer), "LoadRenderModelFB");

            var buffer = new byte[renderModelBuffer.BufferCountOutput];

            fixed (byte* pBuffer = buffer)
            {
                renderModelBuffer.Buffer = pBuffer;
                renderModelBuffer.BufferCapacityInput = renderModelBuffer.BufferCountOutput;

                _app.CheckResult(_modelExt.LoadRenderModelFB(_app.Session, &loadInfo, ref renderModelBuffer), "LoadRenderModelFB");
            }

            return new MemoryStream(buffer, false);
        }

        public void Destroy()
        {
            if (_keyboard.Handle == 0)
                return;

            if (_app.Session.Handle != 0)
                _app.CheckResult(_keyExt!.DestroyVirtualKeyboardMETA!(_keyboard), "DestroyVirtualKeyboardMETA");

            _keyboard.Handle = 0;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }


        public IXrVirtualKeyboardEventDispatcher? EventDispatcher { get; set; }
    }
}
