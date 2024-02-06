using Android.Content;
using Android.Runtime;
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate Silk.NET.OpenXR.Result InitializeLoaderDelegate(LoaderInitInfoAndroidKHR* loader);

        InitializeLoaderDelegate? InitializeLoader;
       

        public AndroidXrPlugin(Context context)
        {
            _context = context;
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

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;

            extensions.Add(KhrLoaderInit.ExtensionName);

            var func = new PfnVoidFunction();
            XrApp.CheckResult(_app.Xr.GetInstanceProcAddr(new Instance(), "xrInitializeLoaderKHR", &func), "Bind xrInitializeLoaderKHR");
            InitializeLoader = Marshal.GetDelegateForFunctionPointer<InitializeLoaderDelegate>(new nint(func.Handle));

            InitAndroid(_context);
        }

    }
}
