using OpenXr.Framework;
using System.Diagnostics;

namespace XrEngine.OpenXr
{
    public class XrFrameCapture : BaseXrComponent<Scene3D>
    {
        private bool _captureRequest;
        private bool _isCapturing;

        protected override void AttachXr()
        {
            Debug.Assert(_xrApp != null);
            _xrApp.BeginFrameEvent += OnBeginFrame;
            _xrApp.EndFrameEvent += OnEndFrame;
        }

        private void OnBeginFrame()
        {
            if (_captureRequest)
            {
                EngineNativeLib.RdcStartFrameCapture();
                _isCapturing = true;
                _captureRequest = false;
            }
        }

        private void OnEndFrame()
        {
            if (_isCapturing)
            {
                EngineNativeLib.RdcEndFrameCapture(true);
                _isCapturing = false;
            }
        }

        [Action]
        public void Capture()
        {
            _captureRequest = true;
        }
    }
}
