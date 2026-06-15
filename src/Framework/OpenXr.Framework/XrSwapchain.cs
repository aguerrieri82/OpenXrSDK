using Common.Interop;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework
{
    public class XrSwapchain : IDisposable
    {
        XrApp _xrApp;
        Swapchain _swapchain;
        long _lastPredictedTime;
        int _usageCount = 0;
        int _curUsageCount = 0;
        uint _lastImage;

        public XrSwapchain(XrApp xrApp, int usageCount)
        {
            _xrApp = xrApp;
            _usageCount = usageCount;
        }

        public void Create(Extent2Di size, long format, uint arraySize, SwapchainUsageFlags usage, uint sampleCount, bool mainSwapChain)
        {
            if (_swapchain.Handle != 0)
                _xrApp.DestroySwapchain(_swapchain);

            _swapchain = _xrApp.CreateSwapChain(size, format, arraySize, usage, sampleCount, mainSwapChain);
            _lastPredictedTime = 0;
        }

        public NativeArray<SwapchainImageBaseHeader> EnumerateImages()
        {
            return _xrApp.EnumerateSwapchainImages(_swapchain);
        }

        public uint Acquire()
        {
            var isNewFrame = _lastPredictedTime != _xrApp.FramePredictedDisplayTime;

            if (isNewFrame)
            {
                _curUsageCount = 0;
                _lastImage = _xrApp.AcquireSwapchainImage(_swapchain);
                _lastPredictedTime = _xrApp.FramePredictedDisplayTime;
            }
            
            _curUsageCount++;

            return _lastImage;
        }

        public void Wait()
        {
            if (_curUsageCount == 1)
                _xrApp.WaitSwapchainImage(_swapchain);
        }

        public void Release()
        {
            if (_curUsageCount == _usageCount)
                _xrApp.ReleaseSwapchainImage(_swapchain);
        }

        public void Dispose()
        {
            if (_swapchain.Handle != 0)
            {
                _xrApp.DestroySwapchain(_swapchain);
                _swapchain.Handle = 0;
            }
        }

        public static implicit operator Swapchain (XrSwapchain self)
        {
            return self._swapchain;
        }

        public bool IsCreated => _swapchain.Handle != 0;

        public Swapchain Value => _swapchain;
    }
}
