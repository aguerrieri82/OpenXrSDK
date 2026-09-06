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

#ifdef _MSC_VER
	using half = uint16_t;
#endif

#if HAS_NEON == 1 
	using half = float16_t;
#endif