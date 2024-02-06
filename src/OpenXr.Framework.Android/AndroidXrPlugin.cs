using Android.Content;
using Android.Runtime;
using Android.Transitions;
using Java.Interop;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{
    public unsafe class AndroidXrPlugin : BaseXrPlugin
    {
        protected XrApp? _app;
        protected Context _context;
        protected KhrAndroidThreadSettings? _thread;
        protected uint _mainThreadId;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate Silk.NET.OpenXR.Result InitializeLoaderDelegate(LoaderInitInfoAndroidKHR* loader);

        InitializeLoaderDelegate? InitializeLoader;
       

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

            XrApp.CheckResult(InitializeLoader!(&android), "InitializeLoader");


        }

        void SetAndroidApplicationThread(AndroidThreadTypeKHR type, uint threadId)
        {
            XrApp.CheckResult(_thread!.SetAndroidApplicationThread(_app!.Session!, type, threadId), "SetAndroidApplicationThread");
        }

        public override void OnInstanceCreated()
        {
            _app!.Xr.TryGetInstanceExtension<KhrAndroidThreadSettings>(null, _app.Instance, out _thread);


            base.OnInstanceCreated();
        }

        public override void OnSessionCreated()
        {
            SetAndroidApplicationThread(AndroidThreadTypeKHR.ApplicationMainKhr, _mainThreadId);
            //SetAndroidApplicationThread(AndroidThreadTypeKHR.ApplicationWorkerKhr, _mainThreadId);
            //SetAndroidApplicationThread(AndroidThreadTypeKHR.RendererWorkerKhr, _mainThreadId);
            SetAndroidApplicationThread(AndroidThreadTypeKHR.RendererMainKhr, _mainThreadId);
            base.OnSessionCreated();
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;

            extensions.Add(KhrLoaderInit.ExtensionName);
            extensions.Add(KhrAndroidThreadSettings.ExtensionName);

            var func = new PfnVoidFunction();
            XrApp.CheckResult(_app.Xr.GetInstanceProcAddr(new Instance(), "xrInitializeLoaderKHR", &func), "Bind xrInitializeLoaderKHR");
            InitializeLoader = Marshal.GetDelegateForFunctionPointer<InitializeLoaderDelegate>(new nint(func.Handle));

            InitAndroid(_context);
        }

    }
}
