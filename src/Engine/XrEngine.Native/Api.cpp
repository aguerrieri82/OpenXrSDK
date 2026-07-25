#include "pch.h"


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
