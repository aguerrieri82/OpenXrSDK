#pragma once

#if _WINDOWS

#define WIN32_LEAN_AND_MEAN           

	#include <windows.h>

	#define close(a) { }

	#define dup(a) { }

	#define setenv(a,b,c) { }

	#include <stdio.h>
	#define DBGPRINTF(...) printf(__VA_ARGS__)

#else
	#include <unistd.h>
	#include <android/log.h>
	#define DBGPRINTF(...) __android_log_print(ANDROID_LOG_DEBUG, "UsbCameraNative", __VA_ARGS__)
#endif

#include "../../../libs/libuvc/include/libuvc.h"
#include "../../../libs/libuvc/include/libusb.h"

#include <vector>
#include <string>
#include <cstring>
#include <mutex>
