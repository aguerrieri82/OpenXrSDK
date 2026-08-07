using Android.Content;
using Android.OS;
using Android.Runtime;
using Common.Interop;
using Java.Interop;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Android
{
    public unsafe class AndroidXrPlugin : XrBasePlugin
    {
        protected Context _context;
        protected KhrAndroidThreadSettings? _thread;
        protected uint _mainThreadId;

        protected NativeStruct<LoaderInitInfoAndroidKHR> _loaderInit;

        protected NativeStruct<InstanceCreateInfoAndroidKHR> _createInfo;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate Silk.NET.OpenXR.Result InitializeLoaderDelegate(LoaderInitInfoAndroidKHR* loader);

        InitializeLoaderDelegate? InitializeLoader;


        public AndroidXrPlugin(Context context)
            : this(context, (uint)Process.MyTid())
        {

        }

        public AndroidXrPlugin(Context context, uint mainThreadId)
        {
            _context = context;
            _mainThreadId = mainThreadId;
        }

        protected void InitAndroid(Context context)
        {
            JniEnvironment.References.GetJavaVM(out var javaVm);

            _loaderInit.Value = new LoaderInitInfoAndroidKHR
            {
                Type = StructureType.LoaderInitInfoAndroidKhr,
                ApplicationContext = (void*)((IJavaObject)context).Handle,
                ApplicationVM = (void*)javaVm,
                Next = null
            };

            _app!.CheckResult(InitializeLoader!(_loaderInit.Pointer), "InitializeLoader");
        }

        void SetAndroidApplicationThread(AndroidThreadTypeKHR type, uint threadId)
        {
            _app!.CheckResult(_thread!.SetAndroidApplicationThread(_app!.Session!, type, threadId), "SetAndroidApplicationThread");
        }


        public override void OnInstanceCreated()
        {
            _app!.Xr.TryGetInstanceExtension<KhrAndroidThreadSettings>(null, _app.Instance, out _thread);
            base.OnInstanceCreated();
        }

        public override void OnSessionCreated()
        {
            SetAndroidApplicationThread(AndroidThreadTypeKHR.ApplicationMainKhr, _mainThreadId);
            SetAndroidApplicationThread(AndroidThreadTypeKHR.RendererMainKhr, _mainThreadId);
            base.OnSessionCreated();
        }

        public override void CreateInstance(ref InstanceCreateInfo info)
        {
            _createInfo.Value = new InstanceCreateInfoAndroidKHR
            {
                Type = StructureType.InstanceCreateInfoAndroidKhr,
                ApplicationVM = _loaderInit.ValueRef.ApplicationVM,
                ApplicationActivity = _loaderInit.ValueRef.ApplicationContext
            };

            StructChain.AddNextStruct(ref info, _createInfo.Pointer);
        }

        public override void ConfigureSwapchain(ref SwapchainCreateInfo info, bool mainSwapChain)
        {
            if (!IsMetaQuest)
                info.UsageFlags &= ~SwapchainUsageFlags.InputAttachmentBitKhr;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;

            extensions.Add(KhrLoaderInit.ExtensionName);
            extensions.Add(KhrAndroidThreadSettings.ExtensionName);
            extensions.Add("XR_EXT_performance_settings");
            extensions.Add("XR_KHR_android_create_instance");

            var func = new PfnVoidFunction();
            _app!.CheckResult(_app.Xr.GetInstanceProcAddr(new Instance(), "xrInitializeLoaderKHR", &func), "Bind xrInitializeLoaderKHR");
            InitializeLoader = Marshal.GetDelegateForFunctionPointer<InitializeLoaderDelegate>(new nint(func.Handle));

            InitAndroid(_context);
        }


        public static bool IsMetaQuest =
                string.Equals(Build.Manufacturer, "Oculus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Build.Manufacturer, "Meta", StringComparison.OrdinalIgnoreCase) ||
                (Build.Model?.Contains("Quest", StringComparison.OrdinalIgnoreCase) == true);
    }
}
