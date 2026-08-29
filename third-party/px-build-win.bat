@echo off
setlocal

set "PHYSX_ROOT=D:\Development\Personal\Git\XrSDK\third-party\physx-rs\physx-sys\physx\physx"
set "BUILD_DIR=%PHYSX_ROOT%\compiler\win64-release"
set "OUTPUT_DIR=%PHYSX_ROOT%\bin-win64"

set "PM_CMakeModules_PATH=D:\packman-repo\chk\CMakeModules\1.28.trunk.32494385"
set "PM_PhysXDevice_PATH=D:\packman-repo\chk\PhysXDevice\18.12.7.4"
set "PM_freeglut_PATH=D:\packman-repo\chk\freeglut-windows\3.4_1.1"
set "PM_PhysXGpu_PATH=D:\packman-repo\chk\PhysXGpu\104.2-5.1.264.32487460-public"

set "PM_PATHS=%PM_CMakeModules_PATH%;D:\packman-repo\chk\clang-physxmetadata\4.0.0.32489833_1;D:\packman-repo\chk\VsWhere\2.7.3111.17308_1.0;%PM_PhysXDevice_PATH%;%PM_freeglut_PATH%;%PM_PhysXGpu_PATH%;D:\packman-repo\chk\rapidjson\1.1.0-67fac85-073453e1"

call "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\Tools\VsDevCmd.bat" -arch=x64 -host_arch=x64
if errorlevel 1 exit /b 1

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

cmake ^
  -S "%PHYSX_ROOT%\source\compiler\cmake" ^
  -B "%BUILD_DIR%" ^
  -G "NMake Makefiles" ^
  -DCMAKE_BUILD_TYPE=release ^
  -DCMAKE_PREFIX_PATH="%PM_PATHS%" ^
  -DCMAKEMODULES_PATH="%PM_CMakeModules_PATH%" ^
  -DTARGET_BUILD_PLATFORM=windows ^
  -DPX_OUTPUT_ARCH=x64 ^
  -DPHYSX_ROOT_DIR="%PHYSX_ROOT%" ^
  -DPX_OUTPUT_LIB_DIR="%OUTPUT_DIR%" ^
  -DPX_OUTPUT_BIN_DIR="%OUTPUT_DIR%" ^
  -DPX_GENERATE_STATIC_LIBRARIES=OFF ^
  -DPX_BUILDSNIPPETS=OFF ^
  -DPX_BUILDPVDRUNTIME=OFF ^
  -DNV_USE_STATIC_WINCRT=OFF ^
  -DNV_USE_DEBUG_WINCRT=OFF ^
  -DPUBLIC_RELEASE=1

if errorlevel 1 exit /b 1

cmake --build "%BUILD_DIR%"
if errorlevel 1 exit /b 1