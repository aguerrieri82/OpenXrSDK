#pragma once

#ifdef _WINDOWS

	#pragma comment(lib, "winmm.lib")

	#define EXPORT __declspec(dllexport)

	#define APIENTRY __cdecl

#else

	#define EXPORT __attribute__((visibility("default")))

	#define APIENTRY

#endif