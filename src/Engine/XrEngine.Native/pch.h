#pragma once

#ifdef _WINDOWS
	#define NOMINMAX
	#include <windows.h>
	#include <mmsystem.h>
#endif

#if defined(__ANDROID__)
	#include <dlfcn.h>
#endif


#include <iostream>
#include <complex>
#include <cmath>
#include <chrono>
#include <thread>
#include <algorithm>
#include <cstdint>
#include <vector>

#include "Config.h"
#include "renderdoc_app.h"
#include "MeshVoxelizer.h"
#include "Api.h"
