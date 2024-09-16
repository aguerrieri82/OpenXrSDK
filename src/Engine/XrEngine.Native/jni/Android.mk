LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := xrengine-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..

LOCAL_SRC_FILES	:= 	$(wildcard $(LOCAL_PATH)/../*.cpp) 

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)
					
include $(BUILD_SHARED_LIBRARY)
