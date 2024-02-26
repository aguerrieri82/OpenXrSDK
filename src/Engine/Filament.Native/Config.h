#pragma once

#ifdef _WINDOWS

	#define EXPORT __declspec(dllexport)

	#ifdef _DLL
		#ifdef _DEBUG
			#define FL_LINK "mdd"
		#else
			#define FL_LINK "md"
		#endif
	#else
		#ifdef _DEBUG
			#define FL_LINK "mtd"
		#else
			#define FL_LINK "mt"
		#endif
	#endif


	#define FL_LIB(name) FL_LINK  "/"  name

	#pragma comment(lib, FL_LIB("filament.lib"))
	#pragma comment(lib, FL_LIB("filamat.lib"))
	#pragma comment(lib, FL_LIB("backend.lib"))
	#pragma comment(lib, FL_LIB("utils.lib"))
	#pragma comment(lib, FL_LIB("filaflat.lib"))
	#pragma comment(lib, FL_LIB("ibl.lib"))
	//#pragma comment(lib, FL_LIB("ibl-lite.lib"))
	#pragma comment(lib, FL_LIB("bluegl.lib"))
	#pragma comment(lib, FL_LIB("bluevk.lib"))
	#pragma comment(lib, FL_LIB("geometry.lib"))
	#pragma comment(lib, FL_LIB("smol-v.lib"))
	#pragma comment(lib, FL_LIB("filabridge.lib"))
	#pragma comment(lib, FL_LIB("shaders.lib"))


	#pragma comment(lib, "opengl32.lib")

#else

	#define EXPORT __attribute__((visibility("default")))

	#define APIENTRY

#endif

using namespace filament;
using namespace filament::backend;
using namespace filament::math;
using namespace filamat;
using namespace utils;
