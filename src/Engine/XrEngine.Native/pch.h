#pragma once

#ifdef _WINDOWS
	#define NOMINMAX
	#include <windows.h>
	#include <mmsystem.h>
#endif

#if defined(__ANDROID__)
	#include <dlfcn.h>
#endif

#if defined(__ARM_NEON) || defined(__ARM_NEON__)
	#include <arm_neon.h>
	#define HAS_NEON 1
#else
	#define HAS_NEON 0
#endif

#if defined(__SSE2__) || defined(_M_X64) || (defined(_M_IX86_FP) && _M_IX86_FP >= 2)
	#include <emmintrin.h>
	#define HAS_SSE2 1
#else
	#define HAS_SSE2 0
#endif

#ifdef _MSC_VER
	#include <immintrin.h>
#endif

#include <iostream>
#include <complex>
#include <cmath>
#include <chrono>
#include <thread>
#include <algorithm>
#include <cstdint>
#include <vector>
#include <mutex>

#include "Config.h"
#include "XrMath.h"
#include "Structs.h"
#include "renderdoc_app.h"
#include "MeshVoxelizer.h"
#include "VoxelLightBaker.h"
#include "Api.h"


