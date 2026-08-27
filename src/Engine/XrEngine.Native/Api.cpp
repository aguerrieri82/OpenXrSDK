#include "pch.h"




#ifdef _MSC_VER
    using half = uint16_t;

    static inline void ToHalf2(const Vec2& src, half* dst)
    {
        __m128 v = _mm_setr_ps(src.X, src.Y, 0.0f, 0.0f);
        __m128i h = _mm_cvtps_ph(v, 0);
        *(uint32_t*)dst = (uint32_t)_mm_cvtsi128_si32(h);
    }
#endif

#if HAS_NEON == 1 

    using half = float16_t;

    static inline void ToHalf2(const Vec2& src, half* dst)
    {
        float32x2_t v = { src.X, src.Y };
        float16x4_t h = vcvt_f16_f32(vcombine_f32(v, vdup_n_f32(0.0f)));

        vst1_lane_f16(dst, h, 0);
        vst1_lane_f16(dst + 1, h, 1);
    }
#endif


void CopyMemory2(uint8_t* src, uint8_t* dst, uint32_t size)
{
	memcpy(dst, src, size);
} 


int CompareMemory(uint8_t* src, uint8_t* dst, uint32_t size)
{
    return memcmp(dst, src, size);
}

void SleepUntil(uint64_t time)
{
	auto duration= std::chrono::nanoseconds(time);

	auto timePoint = std::chrono::time_point<std::chrono::high_resolution_clock>(duration);

	std::this_thread::sleep_until(timePoint);
}


#if defined(_WINDOWS)

    void SleepFor(uint64_t time) {

        uint32_t ms = static_cast<uint32_t>((time + 999999) / 1000000);
        if (ms == 0)
            ms = 1;

        HANDLE evt = CreateEvent(nullptr, FALSE, FALSE, nullptr);
        if (!evt)
        {
            Sleep(ms);
            return;
        }

        MMRESULT id = timeSetEvent(
            ms,
            1,                 
            (LPTIMECALLBACK)evt,
            0,
            TIME_ONESHOT | TIME_CALLBACK_EVENT_SET
        );

        if (id == 0)
        {
            CloseHandle(evt);
            Sleep(ms);             
            return;
        }


        WaitForSingleObject(evt, INFINITE);

        timeKillEvent(id);
        CloseHandle(evt);
    }

#elif defined(__ANDROID__)

    void SleepFor(uint64_t nanoseconds)
    {
        timespec duration
        {
            .tv_sec = static_cast<time_t>(nanoseconds / 1'000'000'000ull),
            .tv_nsec = static_cast<long>(nanoseconds % 1'000'000'000ull)
        };

        while (nanosleep(&duration, &duration) == -1 && errno == EINTR)
        {
        }
    }

#else 

    void SleepFor(uint64_t time)
    {
	    auto duration = std::chrono::nanoseconds(time);

	    std::this_thread::sleep_for(duration);
    }

#endif

uint64_t Now() {

	auto now = std::chrono::high_resolution_clock::now();

	auto duration = now.time_since_epoch();

	return std::chrono::duration_cast<std::chrono::nanoseconds>(duration).count();
}

static RENDERDOC_API_1_6_0* GetRenderDoc()
{

#ifdef _WINDOWS

    HMODULE mod = GetModuleHandleA("renderdoc.dll"); // do NOT LoadLibrary here

    if (!mod)
        return nullptr;

    auto getApi = (pRENDERDOC_GetAPI)GetProcAddress(mod, "RENDERDOC_GetAPI");

    if (!getApi)
        return nullptr;

#else
    
    void* mod = nullptr;

    auto getApi = (pRENDERDOC_GetAPI)dlsym(RTLD_DEFAULT, "RENDERDOC_GetAPI");

    if (!getApi)
    {
        mod = dlopen("librenderdoc.so", RTLD_NOW | RTLD_GLOBAL | RTLD_NOLOAD);

        if (!mod)
            mod = dlopen("libVkLayer_GLES_RenderDoc.so", RTLD_NOW | RTLD_GLOBAL | RTLD_NOLOAD);

        if (!mod)
            return nullptr;

        getApi = (pRENDERDOC_GetAPI)dlsym(mod, "RENDERDOC_GetAPI");
    }

    if (!getApi)
        return nullptr;

#endif

    RENDERDOC_API_1_6_0* rdoc = nullptr;

    if (getApi(eRENDERDOC_API_Version_1_6_0, (void**)&rdoc) != 1)
        return nullptr;
     
    return rdoc;
 
}

int RdcTriggerCapture() {

    auto rdoc = GetRenderDoc();

    if (rdoc) {
        rdoc->TriggerCapture();
        return 0;
    }
    return -1;
}

int RdcStartFrameCapture() {

    auto rdoc = GetRenderDoc();
    if (rdoc) {
        rdoc->StartFrameCapture(nullptr, nullptr);
        return 0;
    }
    return -1;
}


int RdcEndFrameCapture(bool launchReplay) {

    auto rdoc = GetRenderDoc();

    if (rdoc) 
    {
        rdoc->EndFrameCapture(nullptr, nullptr);

        if (launchReplay) {
            if (!rdoc->IsTargetControlConnected())
                rdoc->LaunchReplayUI(1, nullptr);
            else
                rdoc->ShowReplayUI();
        }

        return 0;
    }

    return -1;
}

bool RdcIsAttached()
{
    return GetRenderDoc() != nullptr;
}


void CompressVertices(const VertexData* src, CompVertexData* dst, int count, VertexComponent activeComponents, Bounds3 bounds)
{
    Vec3 size = bounds.Max - bounds.Min;
    Vec3 invSize =
    {
        size.X != 0.0f ? 1.0f / size.X : 0.0f,
        size.Y != 0.0f ? 1.0f / size.Y : 0.0f,
        size.Z != 0.0f ? 1.0f / size.Z : 0.0f
    };

    if (activeComponents & VertexComponent::Position)
    {
        for (int i = 0; i < count; i++)
        {
            Vec3 pos = (src[i].Pos - bounds.Min) * invSize;

            dst[i].Pos[0] = (uint16_t)std::round(std::clamp(pos.X, 0.0f, 1.0f) * UINT16_MAX);
            dst[i].Pos[1] = (uint16_t)std::round(std::clamp(pos.Y, 0.0f, 1.0f) * UINT16_MAX);
            dst[i].Pos[2] = (uint16_t)std::round(std::clamp(pos.Z, 0.0f, 1.0f) * UINT16_MAX);
        }
    }

    if (activeComponents & VertexComponent::Normal)
    {
        for (int i = 0; i < count; i++)
        {
            dst[i].Normal[0] = (int16_t)std::round(src[i].Normal.X * INT16_MAX);
            dst[i].Normal[1] = (int16_t)std::round(src[i].Normal.Y * INT16_MAX);
            dst[i].Normal[2] = (int16_t)std::round(src[i].Normal.Z * INT16_MAX);
        }
    }

    if (activeComponents & VertexComponent::UV0)
    {
        for (int i = 0; i < count; i++)
            ToHalf2(src[i].UV, dst[i].UV);
    }

    if (activeComponents & VertexComponent::UV1)
    {
        for (int i = 0; i < count; i++)
            ToHalf2(src[i].UV1, dst[i].UV1);
    }

    if (activeComponents & VertexComponent::Tangent)
    {
        for (int i = 0; i < count; i++)
        {
            dst[i].Tangent[0] = (int16_t)std::round(src[i].Tangent.X * INT16_MAX);
            dst[i].Tangent[1] = (int16_t)std::round(src[i].Tangent.Y * INT16_MAX);
            dst[i].Tangent[2] = (int16_t)std::round(src[i].Tangent.Z * INT16_MAX);
            dst[i].Tangent[3] = (int16_t)std::round(src[i].Tangent.W * INT16_MAX);
        }
    }
}

void CompressIndices16(const uint32_t* src, uint16_t* dst, int count)
{
    for (int i = 0; i < count; i++)
        dst[i] = (uint16_t)src[i];
}

void CompressIndices8(const uint32_t* src, uint8_t* dst, int count)
{
    for (int i = 0; i < count; i++)
        dst[i] = (uint8_t)src[i];
}