using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.Interop;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{
    public unsafe class AndroidXrPlugin : XrBasePlugin
    {
        protected Context _context;
        protected KhrAndroidThreadSettings? _thread;
        protected uint _mainThreadId;

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
            JniEnvironment.References.GetJavaVM(out nint javaVm);

            var android = new LoaderInitInfoAndroidKHR
            {
                Type = StructureType.LoaderInitInfoAndroidKhr,
                ApplicationContext = (void*)((IJavaObject)context).Handle,
                ApplicationVM = (void*)javaVm
            };

            _app!.CheckResult(InitializeLoader!(&android), "InitializeLoader");
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

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;

            extensions.Add(KhrLoaderInit.ExtensionName);
            extensions.Add(KhrAndroidThreadSettings.ExtensionName);

            var func = new PfnVoidFunction();
            _app!.CheckResult(_app.Xr.GetInstanceProcAddr(new Instance(), "xrInitializeLoaderKHR", &func), "Bind xrInitializeLoaderKHR");
            InitializeLoader = Marshal.GetDelegateForFunctionPointer<InitializeLoaderDelegate>(new nint(func.Handle));

            InitAndroid(_context);
        }

    }
}
