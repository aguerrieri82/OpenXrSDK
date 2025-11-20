LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

BULLET3_LIBS :=  ../../../../libs/bullet3/android-arm64

LOCAL_MODULE := libBussIK
LOCAL_SRC_FILES := $(BULLET3_LIBS)/libBussIK.a
include $(PREBUILT_STATIC_LIBRARY)


#---

include $(CLEAR_VARS)

LOCAL_MODULE := bullet-native

LOCAL_SRC_FILES	:= 	$(wildcard $(LOCAL_PATH)/../*.cpp) 

LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

LOCAL_STATIC_LIBRARIES := libBussIK
					
include $(BUILD_SHARED_LIBRARY)
