LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := xrengine-native

LOCAL_C_INCLUDES := $(LOCAL_PATH)/..

LOCAL_SRC_FILES	:= 	$(wildcard $(LOCAL_PATH)/../*.cpp) 
					
include $(BUILD_SHARED_LIBRARY)
