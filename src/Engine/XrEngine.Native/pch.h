#pragma once

#ifdef _WINDOWS
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

#include "Config.h"
#include "renderdoc_app.h"
#include "Api.h"

