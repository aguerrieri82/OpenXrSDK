#include "pch.h"
#include <backend/DriverEnums.h>
#include <backend/Platform.h>
#include <backend/platforms/PlatformWGL.h>
#include <backend/platforms/OpenGLPlatform.h>


void Initialize(InitializeOptions& options) {

 
    auto engine = Engine::Builder()
        .backend(Backend::OPENGL)
        .build();
    
    auto plat = reinterpret_cast<PlatformWGL*>(engine->getPlatform());

	auto scene = engine->createScene();
	auto renderer = engine->createRenderer();



}


#ifdef _WINDOWS

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#endif 
