#pragma once

#ifdef _WINDOWS
	#define NOMINMAX
	#include <windows.h>
#endif

#include <stdint.h>

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <new>

#include "foundation/PxMath.h"
#include "foundation/PxQuat.h"
#include "foundation/PxTransform.h"
#include "foundation/PxVec3.h"

#include "PxMaterial.h"
#include "PxPhysics.h"
#include "PxRigidDynamic.h"
#include "PxScene.h"
#include "PxShape.h"

#include "geometry/PxBoxGeometry.h"

#include "extensions/PxRigidActorExt.h"
#include "extensions/PxRigidBodyExt.h"

#ifndef PHYSX_SDK_VERSION
#define PHYSX_SDK_VERSION 590
#endif

#if PHYSX_SDK_VERSION >= 590
#include "vehicle/PxVehicleAPI.h"

// PhysX 5.9 folded Vehicle2 into the main physx namespace. Keep the existing
// bridge source compatible with the current Windows SDK spelling.
namespace vehicle2 = physx;
#else
#include "vehicle2/PxVehicleAPI.h"
#endif

#include "Vehicle.h"


#ifdef _WINDOWS

#define PHYSX_LIB_PATH "..\\..\\..\\third-party\\physx-rs\\physx-sys\\physx\\physx\\lib\\bin\\win.x86_64.vc143.md\\release\\"

#pragma comment(lib, PHYSX_LIB_PATH "PhysX_64.lib")
#pragma comment(lib, PHYSX_LIB_PATH "PhysXCommon_64.lib")
#pragma comment(lib, PHYSX_LIB_PATH "PhysXFoundation_64.lib")
#pragma comment(lib, PHYSX_LIB_PATH "PhysXExtensions_static_64.lib")
#pragma comment(lib, PHYSX_LIB_PATH "PhysXCooking_64.lib") 
#pragma comment(lib, PHYSX_LIB_PATH "PhysXVehicle_static_64.lib")

#endif
