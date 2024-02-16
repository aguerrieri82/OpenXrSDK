#------------

LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := dracodec 

LOCAL_SRC_FILES := $(LOCAL_PATH)/arm64-v8a/libdracodec.a

include $(PREBUILT_STATIC_LIBRARY)

#------------

#NDK_LIBS_OUT 
#NDK_OUT  

include $(CLEAR_VARS)

LOCAL_MODULE := draco-native

DRACO_SDK :=  $(LOCAL_PATH)/../../../packages/draco.CPP.1.3.3.1/build/native

LOCAL_C_INCLUDES := $(DRACO_SDK)/include

LOCAL_SRC_FILES	:= 	$(LOCAL_PATH)/../DracoMeshDecoder.cpp
					
LOCAL_STATIC_LIBRARIES := dracodec

LOCAL_LDLIBS := -lc++_static -lc++abi

include $(BUILD_SHARED_LIBRARY)
