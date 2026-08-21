#include "pch.h"


struct CameraState
{
    std::mutex lock;

    uvc_context_t* ctx = nullptr;

    uvc_device_t** devices = nullptr;
    int deviceCount = 0;

    uvc_device_handle_t* devh = nullptr;

    uvc_stream_handle_t* strmh = nullptr;
    uvc_stream_ctrl_t ctrl{};

    std::vector<FormatInfo> formats;

    uvc_frame_t* lastFrame = nullptr;
    std::string lastError;

    libusb_context* usbCtx = nullptr;
    libusb_device_handle* usbHandle = nullptr;
    int ownedFd = -1;
};

static void SetError(CameraState* state, const char* text)
{
    if (!state)
        return;

    state->lastError = text ? text : "";
}

static void SetUvcError(CameraState* state, const char* prefix, int res)
{
    if (!state)
        return;

    state->lastError.clear();

    if (prefix)
    {
        state->lastError += prefix;
        state->lastError += ": ";
    }

    state->lastError += uvc_strerror((uvc_error_t)res);
}

static void CopyString(char* dst, int dstSize, const char* src)
{
    if (!dst || dstSize <= 0)
        return;

    if (!src)
        src = "";

#ifdef _WIN32
    strncpy_s(dst, dstSize, src, _TRUNCATE);
#else
    std::strncpy(dst, src, dstSize - 1);
    dst[dstSize - 1] = 0;
#endif
}

static void CloseStreamInternal(CameraState* state)
{
    if (!state)
        return;

    if (state->strmh)
    {
        uvc_stream_stop(state->strmh);
        uvc_stream_close(state->strmh);
        state->strmh = nullptr;
    }

    state->lastFrame = nullptr;
}

static void CloseDeviceInternal(CameraState* state)
{
    if (!state)
        return;

    CloseStreamInternal(state);

    if (state->devh)
    {
        uvc_close(state->devh);
        state->devh = nullptr;
    }

    if (state->usbHandle)
    {
        libusb_close(state->usbHandle);
        state->usbHandle = nullptr;
    }

#ifndef _WIN32
    if (state->ownedFd >= 0)
    {
        close(state->ownedFd);
        state->ownedFd = -1;
    }
#endif

    state->formats.clear();
}

static void FreeDeviceListInternal(CameraState* state)
{
    if (!state)
        return;

    if (state->devices)
    {
        uvc_free_device_list(state->devices, 1);
        state->devices = nullptr;
        state->deviceCount = 0;
    }
}

EXPORT CameraState* APIENTRY Create()
{
    return new CameraState();
}

EXPORT void APIENTRY Destroy(CameraState* state)
{
    if (!state)
        return;

    {
        std::lock_guard<std::mutex> lock(state->lock);

        CloseDeviceInternal(state);
        FreeDeviceListInternal(state);

        if (state->ctx)
        {
            uvc_exit(state->ctx);
            state->ctx = nullptr;
        }

        SetError(state, "");
    }

    delete state;
}

EXPORT int APIENTRY Init(CameraState* state, bool noDeviceDiscovery, bool enableDebug)
{
    if (!state)
        return -1000;

    if (enableDebug)
    {
#ifndef _WIN32
        setenv("LIBUSB_DEBUG", "4", 1);
#endif

        libusb_set_option(nullptr, LIBUSB_OPTION_LOG_LEVEL, LIBUSB_LOG_LEVEL_DEBUG);
    }

    if (noDeviceDiscovery)
        libusb_set_option(NULL, LIBUSB_OPTION_NO_DEVICE_DISCOVERY, NULL);

    std::lock_guard<std::mutex> lock(state->lock);

    if (state->ctx)
        return 0;

    int res = uvc_init(&state->ctx, nullptr);
    if (res < 0)
    {
        state->ctx = nullptr;
        SetUvcError(state, "uvc_init failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
}

EXPORT void APIENTRY Shutdown(CameraState* state)
{
    if (!state)
        return;

    std::lock_guard<std::mutex> lock(state->lock);

    CloseDeviceInternal(state);
    FreeDeviceListInternal(state);

    if (state->ctx)
    {
        uvc_exit(state->ctx);
        state->ctx = nullptr;
    }

    SetError(state, "");
}

EXPORT int APIENTRY RefreshDevices(CameraState* state)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->ctx)
    {
        SetError(state, "Not initialized");
        return -1;
    }

    CloseDeviceInternal(state);
    FreeDeviceListInternal(state);

    int res = uvc_get_device_list(state->ctx, &state->devices);
    if (res < 0)
    {
        SetUvcError(state, "uvc_get_device_list failed", res);
        return res;
    }

    state->deviceCount = 0;

    while (state->devices[state->deviceCount])
        state->deviceCount++;

    SetError(state, "");
    return state->deviceCount;
}

EXPORT int APIENTRY GetDeviceCount(CameraState* state)
{
    if (!state)
        return 0;

    std::lock_guard<std::mutex> lock(state->lock);
    return state->deviceCount;
}

EXPORT int APIENTRY GetDeviceInfo(CameraState* state, int deviceIndex, DeviceInfo* outInfo)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!outInfo)
    {
        SetError(state, "outInfo is null");
        return -1;
    }

    std::memset(outInfo, 0, sizeof(*outInfo));

    if (!state->devices || deviceIndex < 0 || deviceIndex >= state->deviceCount)
    {
        SetError(state, "Invalid device index");
        return -2;
    }

    uvc_device_descriptor_t* desc = nullptr;

    int res = uvc_get_device_descriptor(state->devices[deviceIndex], &desc);
    if (res < 0)
    {
        SetUvcError(state, "uvc_get_device_descriptor failed", res);
        return res;
    }

    outInfo->index = deviceIndex;
    outInfo->vendorId = desc->idVendor;
    outInfo->productId = desc->idProduct;

    CopyString(outInfo->manufacturer, sizeof(outInfo->manufacturer), desc->manufacturer);
    CopyString(outInfo->product, sizeof(outInfo->product), desc->product);
    CopyString(outInfo->serial, sizeof(outInfo->serial), desc->serialNumber);

    uvc_free_device_descriptor(desc);

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY OpenDevice(CameraState* state, int deviceIndex)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->devices || deviceIndex < 0 || deviceIndex >= state->deviceCount)
    {
        SetError(state, "Invalid device index");
        return -2;
    }

    CloseDeviceInternal(state);

    int res = uvc_open(state->devices[deviceIndex], &state->devh);
    if (res < 0)
    {
        state->devh = nullptr;
        SetUvcError(state, "uvc_open failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY OpenDeviceFd(CameraState* state, int fd, int vendorId, int productId)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->ctx)
    {
        SetError(state, "Not initialized");
        return -1;
    }

    CloseDeviceInternal(state);

#ifdef _WIN32
    SetError(state, "OpenDeviceFd is not supported on Windows");
    return -2;
#else
    int ownedFd = dup(fd);
    if (ownedFd < 0)
    {
        SetError(state, "dup(fd) failed");
        return -2;
    }

    state->ownedFd = ownedFd;

    uvc_error_t res = uvc_wrap(
        state->ownedFd,
        state->ctx,
        &state->devh
    );

    if (res < 0)
    {
        close(state->ownedFd);
        state->ownedFd = -1;
        state->devh = nullptr;

        SetUvcError(state, "uvc_wrap failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
#endif
}

EXPORT void APIENTRY CloseDevice(CameraState* state)
{
    if (!state)
        return;

    std::lock_guard<std::mutex> lock(state->lock);

    CloseDeviceInternal(state);
    SetError(state, "");
}

static int GetFrameFormat(const uvc_format_desc_t* format)
{
    if (!format)
        return UVC_FRAME_FORMAT_UNKNOWN;

    switch (format->bDescriptorSubtype)
    {
    case UVC_VS_FORMAT_MJPEG:
        return UVC_FRAME_FORMAT_MJPEG;

    case UVC_VS_FORMAT_UNCOMPRESSED:
#ifdef UVC_GUID_FORMAT_YUY2
        if (memcmp(format->guidFormat, UVC_GUID_FORMAT_YUY2, 16) == 0)
            return UVC_FRAME_FORMAT_YUYV;
#endif

#ifdef UVC_GUID_FORMAT_NV12
        if (memcmp(format->guidFormat, UVC_GUID_FORMAT_NV12, 16) == 0)
            return UVC_FRAME_FORMAT_NV12;
#endif

        return UVC_FRAME_FORMAT_YUYV;

    case UVC_VS_FORMAT_FRAME_BASED:
        return UVC_FRAME_FORMAT_H264;

    default:
        return UVC_FRAME_FORMAT_UNKNOWN;
    }
}

static int IntervalToFps(uint32_t interval100ns)
{
    if (interval100ns == 0)
        return 0;

    return (int)(10000000.0 / (double)interval100ns + 0.5);
}

EXPORT int APIENTRY RefreshFormats(CameraState* state)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->devh)
    {
        SetError(state, "Device not open");
        return -1;
    }

    state->formats.clear();

    const uvc_format_desc_t* format = uvc_get_format_descs(state->devh);
    int index = 0;

    for (; format; format = format->next)
    {
        const uvc_frame_desc_t* frame = format->frame_descs;

        for (; frame; frame = frame->next)
        {
            FormatInfo info{};
            info.index = index++;

            info.frameFormat = (int)GetFrameFormat(format);
            info.descriptorSubtype = format->bDescriptorSubtype;
            info.formatIndex = format->bFormatIndex;
            info.frameIndex = frame->bFrameIndex;

            info.width = frame->wWidth;
            info.height = frame->wHeight;

            info.defaultFps = IntervalToFps(frame->dwDefaultFrameInterval);
            info.minFps = IntervalToFps(frame->dwMaxFrameInterval);
            info.maxFps = IntervalToFps(frame->dwMinFrameInterval);

            if (frame->dwFrameIntervalStep != 0)
            {
                int fps1 = IntervalToFps(frame->dwMinFrameInterval);
                int fps2 = IntervalToFps(frame->dwMinFrameInterval + frame->dwFrameIntervalStep);
                info.stepFps = abs(fps1 - fps2);
            }
            else
            {
                info.stepFps = 0;
            }

            int fpsCount = 0;

            if (frame->intervals)
            {
                while (frame->intervals[fpsCount] != 0)
                    fpsCount++;
            }
            else if (frame->dwMinFrameInterval &&
                frame->dwMaxFrameInterval &&
                frame->dwFrameIntervalStep)
            {
                for (uint32_t v = frame->dwMinFrameInterval;
                    v <= frame->dwMaxFrameInterval;
                    v += frame->dwFrameIntervalStep)
                {
                    fpsCount++;

                    if (frame->dwMaxFrameInterval - v < frame->dwFrameIntervalStep)
                        break;
                }
            }
            else if (frame->dwDefaultFrameInterval)
            {
                fpsCount = 1;
            }

            info.fpsCount = fpsCount;

            state->formats.push_back(info);
        }
    }

    SetError(state, "");
    return (int)state->formats.size();
}

EXPORT int APIENTRY GetFormatCount(CameraState* state)
{
    if (!state)
        return 0;

    std::lock_guard<std::mutex> lock(state->lock);
    return (int)state->formats.size();
}

EXPORT int APIENTRY GetFormatInfo(CameraState* state, int formatIndex, FormatInfo* outInfo)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!outInfo)
    {
        SetError(state, "outInfo is null");
        return -1;
    }

    if (formatIndex < 0 || formatIndex >= (int)state->formats.size())
    {
        SetError(state, "Invalid format index");
        return -2;
    }

    *outInfo = state->formats[formatIndex];

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY OpenStreamByFormatIndex(
    CameraState* state,
    int formatIndex,
    uint32_t fps
)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->devh)
    {
        SetError(state, "Device not open");
        return -1;
    }

    if (formatIndex < 0 || formatIndex >= (int)state->formats.size())
    {
        SetError(state, "Invalid format index");
        return -2;
    }

    CloseStreamInternal(state);

    const FormatInfo& info = state->formats[formatIndex];

    if (fps == 0)
        fps = info.defaultFps;

    DBGPRINTF(
        "uvc_get_stream_ctrl_format_size("
        "devh=%p, frameFormat=%d, width=%d, height=%d, "
        "fps=%u, formatIndex=%u, frameIndex=%u, descriptorSubtype=%u)",
        state->devh,
        info.frameFormat,
        info.width,
        info.height,
        fps,
        info.formatIndex,
        info.frameIndex,
        info.descriptorSubtype
    );

    int res = uvc_get_stream_ctrl_format_size(
        state->devh,
        &state->ctrl,
        (uvc_frame_format)info.frameFormat,
        info.width,
        info.height,
        fps
    );

    if (res < 0)
    {
        SetUvcError(state, "uvc_get_stream_ctrl_format_size failed", res);
        return res;
    }

    res = uvc_stream_open_ctrl(state->devh, &state->strmh, &state->ctrl);
    if (res < 0)
    {
        state->strmh = nullptr;
        SetUvcError(state, "uvc_stream_open_ctrl failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY OpenStream(
    CameraState* state,
    int frameFormat,
    int width,
    int height,
    uint32_t fps
)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->devh)
    {
        SetError(state, "Device not open");
        return -1;
    }

    CloseStreamInternal(state);

    int res = uvc_get_stream_ctrl_format_size(
        state->devh,
        &state->ctrl,
        (uvc_frame_format)frameFormat,
        width,
        height,
        fps
    );

    if (res < 0)
    {
        SetUvcError(state, "uvc_get_stream_ctrl_format_size failed", res);
        return res;
    }

    res = uvc_stream_open_ctrl(state->devh, &state->strmh, &state->ctrl);
    if (res < 0)
    {
        state->strmh = nullptr;
        SetUvcError(state, "uvc_stream_open_ctrl failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY StartStream(CameraState* state)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!state->strmh)
    {
        SetError(state, "Stream not open");
        return -1;
    }

    int res = uvc_stream_start(state->strmh, nullptr, nullptr, 0);
    if (res < 0)
    {
        SetUvcError(state, "uvc_stream_start failed", res);
        return res;
    }

    SetError(state, "");
    return 0;
}

EXPORT void APIENTRY StopStream(CameraState* state)
{
    if (!state)
        return;

    std::lock_guard<std::mutex> lock(state->lock);

    if (state->strmh)
        uvc_stream_stop(state->strmh);

    state->lastFrame = nullptr;
    SetError(state, "");
}

EXPORT void APIENTRY CloseStream(CameraState* state)
{
    if (!state)
        return;

    std::lock_guard<std::mutex> lock(state->lock);

    CloseStreamInternal(state);
    SetError(state, "");
}

EXPORT int APIENTRY PullFrame(CameraState* state, int timeoutMs, FrameInfo* outFrame)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (!outFrame)
    {
        SetError(state, "outFrame is null");
        return -1;
    }

    std::memset(outFrame, 0, sizeof(*outFrame));

    if (!state->strmh)
    {
        SetError(state, "Stream not open");
        return -2;
    }

    uvc_frame_t* frame = nullptr;

    int res = uvc_stream_get_frame(
        state->strmh,
        &frame,
        timeoutMs * 1000
    );


    if (res < 0)
    {
        SetUvcError(state, "uvc_stream_get_frame failed", res);
        return res;
    }

    if (!frame)
    {
        SetError(state, "uvc_stream_get_frame returned null frame");
        return -3;
    }

    if (!frame->data || frame->data_bytes == 0)
    {
        SetError(state, "uvc_stream_get_frame returned empty frame");
        return -4;
    }

    state->lastFrame = frame;

    outFrame->width = frame->width;
    outFrame->height = frame->height;
    outFrame->frameFormat = (int)frame->frame_format;
    outFrame->sequence = frame->sequence;
    outFrame->data = (const uint8_t*)frame->data;
    outFrame->dataBytes = (int)frame->data_bytes;


    DBGPRINTF(
        "PullFrame data src=%p out=%p bytes=%d frame=%p outFrame=%p, (%d, %d, %d)",
        frame->data,
        outFrame->data,
        outFrame->dataBytes,
        frame,
        outFrame,
        outFrame->data[0],
        outFrame->data[1],
        outFrame->data[2]
    );

    SetError(state, "");
    return 0;
}

EXPORT int APIENTRY CopyFrame(CameraState* state, uint8_t* dst, int dstBytes, int* outBytesWritten)
{
    if (!state)
        return -1000;

    std::lock_guard<std::mutex> lock(state->lock);

    if (outBytesWritten)
        *outBytesWritten = 0;

    if (!state->lastFrame || !state->lastFrame->data)
    {
        SetError(state, "No frame available");
        return -1;
    }

    if (!dst)
    {
        SetError(state, "Destination buffer is null");
        return -2;
    }

    if (dstBytes < (int)state->lastFrame->data_bytes)
    {
        SetError(state, "Destination buffer too small");
        return -(int)state->lastFrame->data_bytes;
    }

    std::memcpy(dst, state->lastFrame->data, state->lastFrame->data_bytes);

    if (outBytesWritten)
        *outBytesWritten = (int)state->lastFrame->data_bytes;

    SetError(state, "");
    return 0;
}

EXPORT const char* APIENTRY GetLastErrorText(CameraState* state)
{
    if (!state)
        return "Invalid CameraState";

    return state->lastError.c_str();
}