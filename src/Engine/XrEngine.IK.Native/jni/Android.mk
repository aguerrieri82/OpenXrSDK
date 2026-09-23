LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := ik-native

LOCAL_SRC_FILES := ../Api.cpp \
                   ../ThirdParty/BlenderIK/intern/IK_QJacobian.cpp \
                   ../ThirdParty/BlenderIK/intern/IK_QJacobianSolver.cpp \
                   ../ThirdParty/BlenderIK/intern/IK_QSegment.cpp \
                   ../ThirdParty/BlenderIK/intern/IK_QTask.cpp \
                   ../ThirdParty/BlenderIK/intern/IK_Solver.cpp

LOCAL_C_INCLUDES := $(LOCAL_PATH)/.. \
                    $(LOCAL_PATH)/../ThirdParty/Eigen \
                    $(LOCAL_PATH)/../ThirdParty/BlenderIK/intern \
                    $(LOCAL_PATH)/../ThirdParty/BlenderIK/extern

LOCAL_PCH := ../pch.h

LOCAL_CPPFLAGS += -DEIGEN_MPL2_ONLY -D_USE_MATH_DEFINES -Wno-deprecated-declarations
LOCAL_LDFLAGS += $(ANDROID_LD_FLAGS)

include $(BUILD_SHARED_LIBRARY)