#pragma once

#ifdef _WINDOWS

#define EXPORT __declspec(dllexport)

#define APIENTRY __stdcall

#else

#define EXPORT __attribute__((visibility("default")))
#define APIENTRY
#endif


#include <draco/compression/decode.h>
#include "Api.h"