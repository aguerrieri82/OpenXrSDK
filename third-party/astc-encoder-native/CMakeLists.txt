#  SPDX-License-Identifier: Apache-2.0
#  ----------------------------------------------------------------------------
#  Copyright 2021 Arm Limited
#  ----------------------------------------------------------------------------

cmake_minimum_required(VERSION 3.15)
project(astcencoder-native VERSION 1.1.0 LANGUAGES C CXX)

include(ExternalProject)

# --------------------------------------------------------------------------
# Options

option(ASTCENC_TARGET_ARM "Force NEON build and link (useful for cross-compiling)" OFF)

# --------------------------------------------------------------------------
# Locate astc-encoder sources

set(ASTCENC_SOURCE_DIR "${CMAKE_SOURCE_DIR}/../astc-encoder" CACHE PATH "Path to astc-encoder source tree")

# --------------------------------------------------------------------------
# Decide which astcenc library target name we will link against
# (these are the target names produced by astc-encoder's own CMake)

set(ASTCENC_LINK_LIB "astcenc-native-static")
set(ASTCENC_ISA_ARGS)  # list, not string

if (ASTCENC_TARGET_ARM)
    list(APPEND ASTCENC_ISA_ARGS -DASTCENC_ISA_NEON:BOOL=ON)
    set(ASTCENC_LINK_LIB "astcenc-neon-static")
else()
    list(APPEND ASTCENC_ISA_ARGS -DASTCENC_ISA_SSE41:String=ON)
    set(ASTCENC_LINK_LIB "astcenc-sse4.1-static")
endif()

# --------------------------------------------------------------------------
# ExternalProject cache arguments (MUST be a list)
# NOTE: ExternalProject does not inherit your toolchain/Android settings automatically.
# We explicitly forward them when ANDROID is true.

set(ASTCENC_EP_CACHE_ARGS
    -DASTCENC_CLI:BOOL=OFF
    -DCMAKE_BUILD_TYPE:STRING=${CMAKE_BUILD_TYPE}
    -DCMAKE_POSITION_INDEPENDENT_CODE:BOOL=ON
)

# Forward cross-compile settings (Android NDK toolchain)
if (ANDROID)
    list(APPEND ASTCENC_EP_CACHE_ARGS
        -DCMAKE_TOOLCHAIN_FILE:FILEPATH=${CMAKE_TOOLCHAIN_FILE}
        -DANDROID:BOOL=ON
        -DANDROID_ABI:STRING=${ANDROID_ABI}
        -DANDROID_PLATFORM:STRING=${ANDROID_PLATFORM}
        -DANDROID_STL:STRING=${ANDROID_STL}
    )
endif()

# Forward install prefix if set at top-level (optional)
if (DEFINED CMAKE_INSTALL_PREFIX AND NOT CMAKE_INSTALL_PREFIX STREQUAL "")
    list(APPEND ASTCENC_EP_CACHE_ARGS
        -DCMAKE_INSTALL_PREFIX:PATH=${CMAKE_INSTALL_PREFIX}
    )
endif()

# Append ISA args (NEON etc.)
list(APPEND ASTCENC_EP_CACHE_ARGS ${ASTCENC_ISA_ARGS})

# --------------------------------------------------------------------------
# Build astc-encoder

ExternalProject_Add(astcencoder
    SOURCE_DIR "${ASTCENC_SOURCE_DIR}"

    # Put external build in a predictable place
    BINARY_DIR "${CMAKE_BINARY_DIR}/_deps/astcencoder-build"

    # Make sure it does not try to "update" anything
    UPDATE_COMMAND ""

    CMAKE_CACHE_ARGS ${ASTCENC_EP_CACHE_ARGS}

    # We don't need install for this wrapper; we link from the build tree
    INSTALL_COMMAND ""
)

ExternalProject_Get_property(astcencoder SOURCE_DIR)
ExternalProject_Get_property(astcencoder BINARY_DIR)

# --------------------------------------------------------------------------
# Build YOUR shared library (wrapper)

add_library(astcencoder-native SHARED
    main.cpp
)

add_dependencies(astcencoder-native astcencoder)

target_include_directories(astcencoder-native
    PRIVATE
        "${SOURCE_DIR}/Source"
)

# If you can, prefer importing the library by full path.
# For now keep link_directories + target name (your original approach), but at least consistent.
target_link_directories(astcencoder-native
    PRIVATE
        "${BINARY_DIR}/Source"
)

target_link_libraries(astcencoder-native
    PRIVATE
        ${ASTCENC_LINK_LIB}
)

# Optional: on ELF platforms hide symbols by default (export explicitly in your headers)
if (NOT WIN32)
    set_target_properties(astcencoder-native PROPERTIES
        CXX_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN YES
    )
endif()

# --------------------------------------------------------------------------
# Diagnostics (enable temporarily if needed)
# message(STATUS "ANDROID=${ANDROID}")
# message(STATUS "CMAKE_TOOLCHAIN_FILE=${CMAKE_TOOLCHAIN_FILE}")
# message(STATUS "ANDROID_ABI=${ANDROID_ABI}")
# message(STATUS "ANDROID_PLATFORM=${ANDROID_PLATFORM}")
# message(STATUS "ASTCENC_EP_CACHE_ARGS=${ASTCENC_EP_CACHE_ARGS}")
# message(STATUS "ASTCENC_LINK_LIB=${ASTCENC_LINK_LIB}")
