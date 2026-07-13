LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := xrengine-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..

LOCAL_SRC_FILES	:= 	$(wildcard $(LOCAL_PATH)/../*.cpp) 

LOCAL_PCH := ../pch.h

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

LOCAL_CPPFLAGS += $(ANDROID_CPP_FLAGS) -ffast-math
					
include $(BUILD_SHARED_LIBRARY)
