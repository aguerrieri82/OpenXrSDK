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

#include "vehicle2/PxVehicleAPI.h"

#include "Vehicle.h"


#ifdef _WINDOWS

	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysX_64.lib")
	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysXCommon_64.lib")
	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysXFoundation_64.lib")
	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysXExtensions_static_64.lib")
	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysXCooking_64.lib")
	#pragma comment(lib, "..\\..\\..\\libs\\physx-141\\bin\\win-x64\\PhysXVehicle2_static_64.lib")


#endif