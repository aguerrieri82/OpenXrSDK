LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := xrengine-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..
LOCAL_C_INCLUDES += $(LOCAL_PATH)/../../../../third-party/basis_universal/transcoder

LOCAL_SRC_FILES	:= 	$(wildcard $(LOCAL_PATH)/../*.cpp) 
LOCAL_SRC_FILES += ../../../../third-party/basis_universal/transcoder/basisu_transcoder.cpp
LOCAL_SRC_FILES += ../../../../third-party/basis_universal/zstd/zstddeclib.c

LOCAL_PCH := ../pch.h

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

LOCAL_CPPFLAGS += $(ANDROID_CPP_FLAGS) -ffast-math

					
include $(BUILD_SHARED_LIBRARY)