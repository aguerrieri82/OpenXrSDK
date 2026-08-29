@echo off
setlocal

set "PHYSX_REPO=D:\Development\Library\PhysX"
set "PHYSX_ROOT=%PHYSX_REPO%\physx"
set "NDK=C:\Android\Sdk\ndk\30.0.14904198"
set "ANDROID_API=29"
set "BUILD_DIR=%PHYSX_ROOT%\compiler\android-arm64-release-direct-v10"
set "OUTPUT_DIR=%PHYSX_ROOT%\bin-android-arm64"
set "COMPAT_DIR=%PHYSX_ROOT%\compiler\android-compat-v10"
set "COMPAT_HEADER=%COMPAT_DIR%\AndroidPhysXCompat.h"
set "COMPAT_HEADER_TMP=%COMPAT_DIR%\AndroidPhysXCompat.h.tmp"
set "COMPAT_BITS=%COMPAT_DIR%\bits"

if not exist "%NDK%\build\cmake\android.toolchain.cmake" (
    echo Android NDK toolchain not found:
    echo %NDK%\build\cmake\android.toolchain.cmake
    exit /b 1
)

set "PM_CMakeModules_PATH=D:\packman-repo\chk\CMakeModules\1.28.trunk.32494385"
set "PM_CMakeModules_NAME=CMakeModules"
set "PM_CMakeModules_VERSION=1.28.trunk.32494385"

if not exist "%PM_CMakeModules_PATH%" (
    echo CMakeModules not found:
    echo %PM_CMakeModules_PATH%
    exit /b 1
)

set "NINJA="
for /f "delims=" %%I in ('where ninja.exe 2^>nul') do if not defined NINJA set "NINJA=%%I"

if not defined NINJA (
    for /f "delims=" %%I in ('dir /b /s "C:\Android\Sdk\cmake\*\bin\ninja.exe" 2^>nul') do if not defined NINJA set "NINJA=%%I"
)

if not defined NINJA (
    echo ninja.exe not found. Install Ninja or Android SDK CMake.
    exit /b 1
)

echo Using Ninja: %NINJA%

git -C "%PHYSX_REPO%" diff --quiet -- ^
  physx/source/foundation/unix/FdUnixThread.cpp ^
  physx/source/foundation/unix/FdUnixMutex.cpp

if errorlevel 1 (
    git -C "%PHYSX_REPO%" restore --source=HEAD -- ^
      physx/source/foundation/unix/FdUnixThread.cpp ^
      physx/source/foundation/unix/FdUnixMutex.cpp
    if errorlevel 1 exit /b 1
)

if not exist "%COMPAT_BITS%" mkdir "%COMPAT_BITS%"

> "%COMPAT_HEADER_TMP%" echo #pragma once
>> "%COMPAT_HEADER_TMP%" echo.
>> "%COMPAT_HEADER_TMP%" echo #if defined(__ANDROID__)
>> "%COMPAT_HEADER_TMP%" echo #include ^<pthread.h^>
>> "%COMPAT_HEADER_TMP%" echo.
>> "%COMPAT_HEADER_TMP%" echo #ifndef PTHREAD_PRIO_PROTECT
>> "%COMPAT_HEADER_TMP%" echo #define PTHREAD_PRIO_PROTECT PTHREAD_PRIO_INHERIT
>> "%COMPAT_HEADER_TMP%" echo #endif
>> "%COMPAT_HEADER_TMP%" echo.
>> "%COMPAT_HEADER_TMP%" echo static inline int pthread_cancel^(pthread_t^)
>> "%COMPAT_HEADER_TMP%" echo ^{
>> "%COMPAT_HEADER_TMP%" echo     return 0;
>> "%COMPAT_HEADER_TMP%" echo ^}
>> "%COMPAT_HEADER_TMP%" echo.
>> "%COMPAT_HEADER_TMP%" echo static inline int pthread_mutexattr_setprioceiling^(pthread_mutexattr_t*, int^)
>> "%COMPAT_HEADER_TMP%" echo ^{
>> "%COMPAT_HEADER_TMP%" echo     return 0;
>> "%COMPAT_HEADER_TMP%" echo ^}
>> "%COMPAT_HEADER_TMP%" echo #endif

if not exist "%COMPAT_HEADER%" goto update_compat_header
fc /b "%COMPAT_HEADER%" "%COMPAT_HEADER_TMP%" >nul
if errorlevel 1 goto update_compat_header
del "%COMPAT_HEADER_TMP%"
goto compat_header_done

:update_compat_header
move /y "%COMPAT_HEADER_TMP%" "%COMPAT_HEADER%" >nul

:compat_header_done
if not exist "%COMPAT_BITS%\local_lim.h" (
    > "%COMPAT_BITS%\local_lim.h" echo #pragma once
    >> "%COMPAT_BITS%\local_lim.h" echo #include ^<limits.h^>
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

set "COMPAT_DIR_CMAKE=%COMPAT_DIR:\=/%"
set "COMPAT_HEADER_CMAKE=%COMPAT_HEADER:\=/%"

if not exist "%COMPAT_DIR%\librt.a" (
    > "%COMPAT_DIR%\empty_rt.c" echo void physx_android_empty_rt^(void^) { }

    "%NDK%\toolchains\llvm\prebuilt\windows-x86_64\bin\clang.exe" --target=aarch64-none-linux-android%ANDROID_API% --sysroot="%NDK%\toolchains\llvm\prebuilt\windows-x86_64\sysroot" -c "%COMPAT_DIR%\empty_rt.c" -o "%COMPAT_DIR%\empty_rt.o"
    if errorlevel 1 exit /b 1

    "%NDK%\toolchains\llvm\prebuilt\windows-x86_64\bin\llvm-ar.exe" rcs "%COMPAT_DIR%\librt.a" "%COMPAT_DIR%\empty_rt.o"
    if errorlevel 1 exit /b 1
)

cmake ^
  -S "%PHYSX_ROOT%\source\compiler\cmake" ^
  -B "%BUILD_DIR%" ^
  -G "Ninja" ^
  -DCMAKE_MAKE_PROGRAM="%NINJA%" ^
  -DCMAKE_TOOLCHAIN_FILE="%NDK%\build\cmake\android.toolchain.cmake" ^
  -DANDROID_ABI=arm64-v8a ^
  -DANDROID_PLATFORM=android-%ANDROID_API% ^
  -DANDROID_STL=c++_shared ^
  -DCMAKE_BUILD_TYPE=release ^
  -DPHYSX_CXX_FLAGS_RELEASE="-O3 -w -I%COMPAT_DIR_CMAKE% -include %COMPAT_HEADER_CMAKE%" ^
  -DCMAKE_SHARED_LINKER_FLAGS="-L%COMPAT_DIR_CMAKE%" ^
  -DTARGET_BUILD_PLATFORM=linux ^
  -DPHYSX_ROOT_DIR="%PHYSX_ROOT%" ^
  -DPX_OUTPUT_LIB_DIR="%OUTPUT_DIR%" ^
  -DPX_OUTPUT_BIN_DIR="%OUTPUT_DIR%" ^
  -DPX_GENERATE_STATIC_LIBRARIES=OFF ^
  -DPUBLIC_RELEASE=1 ^
  -DGPU_LIB_COPIED=1

if errorlevel 1 exit /b 1

cmake --build "%BUILD_DIR%" -j
if errorlevel 1 exit /b 1

echo.
echo PhysX Android arm64 dynamic Release build completed.
echo Output base: %OUTPUT_DIR%
