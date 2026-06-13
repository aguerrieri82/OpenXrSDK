LOCAL_PATH := $(call my-dir)

# ------------------------------------------------------------
# Prebuilt libuvc
# ------------------------------------------------------------

include $(CLEAR_VARS)

LOCAL_MODULE := libuvc
LOCAL_SRC_FILES := ../../../../libs/libuvc/android-arm64/libuvc.so

# Adjust if your headers are elsewhere
LOCAL_EXPORT_C_INCLUDES := \
    $(LOCAL_PATH)/../../../../libs/libuvc/include

include $(PREBUILT_SHARED_LIBRARY)


# ------------------------------------------------------------
# Prebuilt libuvc
# ------------------------------------------------------------

include $(CLEAR_VARS)

LOCAL_MODULE := libusb
LOCAL_SRC_FILES := ../../../../libs/libuvc/android-arm64/libusb1.0.so

include $(PREBUILT_SHARED_LIBRARY)



# ------------------------------------------------------------
# Your native wrapper
# ------------------------------------------------------------

include $(CLEAR_VARS)

LOCAL_MODULE := usbcamera-native

LOCAL_C_INCLUDES := \
    $(LOCAL_PATH)/.. \
    $(LOCAL_PATH)/../../../../libs/libuvc/include

FILE_LIST := $(wildcard $(LOCAL_PATH)/../*.cpp)
LOCAL_SRC_FILES := $(FILE_LIST:$(LOCAL_PATH)/%=%)

LOCAL_SHARED_LIBRARIES := libuvc libusb

LOCAL_LDLIBS += -llog
LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

# DEBUG

LOCAL_CFLAGS += -g
LOCAL_CPPFLAGS += -g

#

include $(BUILD_SHARED_LIBRARY)