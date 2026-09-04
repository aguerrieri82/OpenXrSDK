LOCAL_PATH := $(call my-dir)

PHYSX_ROOT := $(LOCAL_PATH)/../../../../libs/physx-590
PHYSX_LIB_PATH := $(PHYSX_ROOT)/bin/android-arm64/release


include $(CLEAR_VARS)
LOCAL_MODULE := PhysX
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysX_64.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := PhysXCommon
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysXCommon_64.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := PhysXFoundation
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysXFoundation_64.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := PhysXCooking
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysXCooking_64.so
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := PhysXExtensions_static
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysXExtensions_static_64.a
include $(PREBUILT_STATIC_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := PhysXVehicle_static
LOCAL_SRC_FILES := $(PHYSX_LIB_PATH)/libPhysXVehicle_static_64.a
include $(PREBUILT_STATIC_LIBRARY)


include $(CLEAR_VARS)

LOCAL_MODULE := physics-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..
LOCAL_C_INCLUDES += $(PHYSX_ROOT)/include

LOCAL_SRC_FILES := $(wildcard $(LOCAL_PATH)/../Vehicle.cpp)
LOCAL_SRC_FILES += $(wildcard $(LOCAL_PATH)/../pch.cpp)

LOCAL_PCH := ../pch.h

LOCAL_CPPFLAGS += $(ANDROID_CPP_FLAGS) -ffast-math

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

LOCAL_SHARED_LIBRARIES := PhysX PhysXCommon PhysXFoundation PhysXCooking
LOCAL_STATIC_LIBRARIES := PhysXExtensions_static PhysXVehicle_static

include $(BUILD_SHARED_LIBRARY)
