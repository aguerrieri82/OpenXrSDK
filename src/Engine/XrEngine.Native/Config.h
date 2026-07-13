#pragma once

#ifdef _WINDOWS

	#pragma comment(lib, "winmm.lib")

	#define EXPORT __declspec(dllexport)

	#define FORCE_INLINE __forceinline

#else

	#define FORCE_INLINE inline __attribute__((always_inline))

	#define EXPORT __attribute__((visibility("default")))

	#define APIENTRY

#endif
