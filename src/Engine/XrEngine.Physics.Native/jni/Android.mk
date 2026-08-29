LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := physics-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..
LOCAL_C_INCLUDES += $(LOCAL_PATH)/../../../../libs/physx-141/include

LOCAL_SRC_FILES := $(wildcard $(LOCAL_PATH)/../*.cpp)

LOCAL_PCH := ../pch.h

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)
LOCAL_LDFLAGS += -L$(LOCAL_PATH)/../../../../libs/physx-141/bin/android-arm64/release

LOCAL_CPPFLAGS += $(ANDROID_CPP_FLAGS) -ffast-math

LOCAL_LDLIBS += -lPhysX
LOCAL_LDLIBS += -lPhysXCommon
LOCAL_LDLIBS += -lPhysXFoundation
LOCAL_LDLIBS += -lPhysXCooking
LOCAL_LDLIBS += -lPhysXExtensions_static
LOCAL_LDLIBS += -lPhysXVehicle2_static

include $(BUILD_SHARED_LIBRARY)