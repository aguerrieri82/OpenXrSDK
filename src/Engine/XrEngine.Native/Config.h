#pragma once

#ifdef _WINDOWS

#define EXPORT __declspec(dllexport)

#define APIENTRY __cdecl

#else

#define EXPORT __attribute__((visibility("default")))

#define APIENTRY

#endif