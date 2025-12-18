#pragma once

#ifdef _DEBUG
	#define BULLET_BASE_DIR  D:/Development/Library/bullet3/out/install/x64-Debug
#else
	#define BULLET_BASE_DIR  D:/Development/Library/bullet3/out/install/x64-Release
#endif

#define BULLET_INC_DIR   include/bullet
#define BUSSIK_SUBDIR    ThirdPartyLibs
#define BUSSIK_LIB_DIR   lib

#define STR2(x) #x
#define STR(x) STR2(x)

// Produce ONE string literal: "D:/.../include/bullet/ThirdPartyLibs/file"
#define BUSSIK_HEADER(file) STR(BULLET_BASE_DIR/BULLET_INC_DIR/BUSSIK_SUBDIR/file)

#ifdef _WINDOWS

	#define WIN32_LEAN_AND_MEAN
	#include <windows.h>

	#define EXPORT __declspec(dllexport)

	#ifdef _DEBUG
		#define BL_LIB(name) STR(BULLET_BASE_DIR/BUSSIK_LIB_DIR/name##_Debug.lib)
	#else
		#define BL_LIB(name) STR(BULLET_BASE_DIR/BUSSIK_LIB_DIR/name##_RelWithDebugInfo.lib)
	#endif

	#pragma comment(lib, BL_LIB(BussIK))

#else
	#define EXPORT __attribute__((visibility("default")))
	#define APIENTRY
	#define UINT unsigned int
#endif