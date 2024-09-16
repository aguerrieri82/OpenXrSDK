LOCAL_PATH := $(call my-dir)

FILAMENT_SDK :=  $(LOCAL_PATH)/../../../../libs/filament-android

FILAMENT_LIBS :=  $(FILAMENT_SDK)/lib/arm64-v8a

#------------


LOCAL_MODULE := libfilament
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libfilament.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libutils
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libutils.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libfilamat
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libfilamat.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libfilabridge
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libfilabridge.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libbackend
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libbackend.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libfilaflat
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libfilaflat.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libibl
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libibl.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libbluevk
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libbluevk.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libgeometry
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libgeometry.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libsmol-v
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libsmol-v.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libshaders
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libshaders.a
include $(PREBUILT_STATIC_LIBRARY)

LOCAL_MODULE := libvkshaders
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libvkshaders.a
include $(PREBUILT_STATIC_LIBRARY)


LOCAL_MODULE := libfilament-iblprefilter
LOCAL_SRC_FILES := $(FILAMENT_LIBS)/libfilament-iblprefilter.a
include $(PREBUILT_STATIC_LIBRARY)

#------------

include $(CLEAR_VARS)

LOCAL_MODULE := filament-native

LOCAL_C_INCLUDES := $(FILAMENT_SDK)/include

LOCAL_SRC_FILES	:= 	$(LOCAL_PATH)/../Api.cpp
					
LOCAL_STATIC_LIBRARIES := libfilament libutils libfilamat libfilabridge libbackend libfilaflat libibl libbluevk libgeometry libsmol-v libshaders libvkshaders libfilament-iblprefilter

LOCAL_LDLIBS := -llog -lEGL -lGLESv3 

include $(BUILD_SHARED_LIBRARY)
