#pragma once

#ifdef _WINDOWS

    #define EXPORT __declspec(dllexport)

    #pragma comment(lib, "../../../libs/libuvc/win-x64/uvc.lib")
    #pragma comment(lib, "../../../libs/libuvc/win-x64/libusb-1.0.lib")

#else

    #define EXPORT __attribute__((visibility("default")))  

    #define APIENTRY

#endif

extern "C" {

    struct CameraState;

    struct DeviceInfo
    {
        int index;
        uint16_t vendorId;
        uint16_t productId;
        char manufacturer[256];
        char product[256];
        char serial[128];
    };

    struct FormatInfo
    {
        int index;
        int frameFormat;
        uint8_t descriptorSubtype;
        uint8_t formatIndex;
        uint8_t frameIndex;
        int width;
        int height;
        int fpsCount;
        int defaultFps;
        int minFps;
        int maxFps;
        int stepFps;
    };

    struct FrameInfo
    {
        int width;
        int height;
        int frameFormat;
        uint32_t sequence;
        const uint8_t* data;
        int dataBytes;
    };

    EXPORT CameraState* APIENTRY Create();
    EXPORT void APIENTRY Destroy(CameraState* state);

    EXPORT int APIENTRY Init(CameraState* state, bool noDeviceDiscovery, bool enableDebug);
    EXPORT void APIENTRY Shutdown(CameraState* state);

    EXPORT int APIENTRY RefreshDevices(CameraState* state);
    EXPORT int APIENTRY GetDeviceCount(CameraState* state);
    EXPORT int APIENTRY GetDeviceInfo(CameraState* state, int deviceIndex, DeviceInfo* outInfo);

    EXPORT int APIENTRY OpenDevice(CameraState* state, int deviceIndex);
    EXPORT int APIENTRY OpenDeviceFd(CameraState* state, int fd, int vendorId, int productId);
    EXPORT void APIENTRY CloseDevice(CameraState* state);

    EXPORT int APIENTRY RefreshFormats(CameraState* state);
    EXPORT int APIENTRY GetFormatCount(CameraState* state);
    EXPORT int APIENTRY GetFormatInfo(CameraState* state, int formatIndex, FormatInfo* outInfo);

    EXPORT int APIENTRY OpenStreamByFormatIndex(CameraState* state, int formatIndex, uint32_t fps);
    EXPORT int APIENTRY OpenStream(CameraState* state, int frameFormat, int width, int height, uint32_t fps);

    EXPORT int APIENTRY StartStream(CameraState* state);
    EXPORT void APIENTRY StopStream(CameraState* state);
    EXPORT void APIENTRY CloseStream(CameraState* state);

    EXPORT int APIENTRY PullFrame(CameraState* state, int timeoutMs, FrameInfo* outFrame);
    EXPORT int APIENTRY CopyFrame(CameraState* state, uint8_t* dst, int dstBytes, int* outBytesWritten);

    EXPORT const char* APIENTRY GetLastErrorText(CameraState* state);


}
