using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.OpenXr
{
    public class XrFrameCapture : Behavior<Scene3D>
    {
        private XrApp? _xrApp;
        private bool _captureRequest;
        private bool _isCapturing;

        protected override void Update(RenderContext ctx)
        {
            if (_xrApp != XrApp.Current && XrApp.Current != null)
            {
                _xrApp = XrApp.Current;
                _xrApp.BeginFrameEvent += OnBeginFrame;
                _xrApp.EndFrameEvent += OnEndFrame;
            }
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
