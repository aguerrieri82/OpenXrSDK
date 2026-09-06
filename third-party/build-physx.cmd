@echo off
setlocal

rem Run from this script's directory and load the shared Windows/Android toolchain variables.
pushd "%~dp0"
if errorlevel 1 exit /b 1

call vars.cmd
if errorlevel 1 goto BUILD_FAILED

rem Common SDK, build, and final package locations.
set ROOT=%CD%\..
set PHYSX_PACKAGE=%ROOT%\libs\physx-590
set WIN_SDK=%PHYSX_PACKAGE%\bin\win64-mt\release
set ANDROID_SDK=%PHYSX_PACKAGE%\bin\android-arm64\release
set WIN_PACKAGE=%ROOT%\libs\physx-native\win-x64
set ANDROID_PACKAGE=%ROOT%\libs\physx-native\android-arm64
set NATIVE_SOURCE=%CD%\physx-rs\physx-sys\src
set WIN_BUILD=%NATIVE_SOURCE%\out-win64
set ANDROID_BUILD=%NATIVE_SOURCE%\out-android-arm64
set ANDROID_BINDINGS=%CD%\physx-rs\physx-sys\build\android-bindings

rem Build the Windows PhysX SDK only when the complete PVD-enabled package is missing.
set WIN_SDK_READY=1
for %%F in (
    PhysX_64.dll
    PhysXCommon_64.dll
    PhysXFoundation_64.dll
    PhysXCooking_64.dll
    PhysX_64.lib
    PhysXCommon_64.lib
    PhysXFoundation_64.lib
    PhysXCooking_64.lib
    PhysX_64.pdb
    PhysXCommon_64.pdb
    PhysXFoundation_64.pdb
    PhysXCooking_64.pdb
    PhysXExtensions_static_64.lib
    PhysXPvdSDK_static_64.lib
    PhysXVehicle_static_64.lib
    .pvd-enabled
) do if not exist %WIN_SDK%\%%F set WIN_SDK_READY=0

if %WIN_SDK_READY% == 1 (
    echo PhysX 5.9 Windows package already exists; skipping SDK build.
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File physx-rs\physx-sys\build-windows.ps1 -PackageDirectory %PHYSX_PACKAGE%
    if errorlevel 1 goto BUILD_FAILED
)

rem Build the Android PhysX SDK only when the complete PVD-disabled package is missing.
set ANDROID_SDK_READY=1
for %%F in (
    libPhysX_64.so
    libPhysXCommon_64.so
    libPhysXFoundation_64.so
    libPhysXCooking_64.so
    libPhysXExtensions_static_64.a
    libPhysXPvdSDK_static_64.a
    libPhysXVehicle_static_64.a
    .pvd-disabled
) do if not exist %ANDROID_SDK%\%%F set ANDROID_SDK_READY=0

if %ANDROID_SDK_READY% == 1 (
    echo PhysX 5.9 Android package already exists; skipping SDK build.
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File physx-rs\physx-sys\build-android.ps1 -PackageDirectory %PHYSX_PACKAGE%
    if errorlevel 1 goto BUILD_FAILED
)

rem Reuse generated bindings when available; a fresh checkout generates them once.
set PHYSX_CARGO_OUT=
for /d %%D in (physx-rs\target\release\build\physx-sys-*) do (
    if exist "%%~fD\out\bindings\physx_generated.hpp" set PHYSX_CARGO_OUT=%%~fD\out
)

if not defined PHYSX_CARGO_OUT (
    echo Building physx-rs and generating PhysX 5.9 bindings...
    cargo build --release --manifest-path physx-rs\Cargo.toml
    if errorlevel 1 goto BUILD_FAILED

    for /d %%D in (physx-rs\target\release\build\physx-sys-*) do (
        if exist "%%~fD\out\bindings\physx_generated.hpp" set PHYSX_CARGO_OUT=%%~fD\out
    )

    if not defined PHYSX_CARGO_OUT (
        echo Unable to find the generated PhysX bindings from cargo build.
        goto BUILD_FAILED
    )
) else (
    echo PhysX 5.9 bindings already exist; skipping physx-rs build.
)

rem Regenerate the managed C# declarations from the current physx-sys output.
echo Regenerating the MagicPhysX C# declarations...
cargo check --manifest-path MagicPhysX\src\libmagicphysx\Cargo.toml
if errorlevel 1 goto BUILD_FAILED

rem Build and package the Windows wrapper with the shared PhysX runtime DLLs.
echo Building physx-native.dll...
if not exist %WIN_BUILD% md %WIN_BUILD%
cmake -S %NATIVE_SOURCE% -B %WIN_BUILD% -G Ninja ^
    -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
    -DPHYSX_SDK=%PHYSX_PACKAGE% ^
    "-DPHYSX_GENERATED_DIR=%PHYSX_CARGO_OUT%"
if errorlevel 1 goto BUILD_FAILED

cmake --build %WIN_BUILD%
if errorlevel 1 goto BUILD_FAILED

if not exist %WIN_PACKAGE% md %WIN_PACKAGE%
copy /Y %WIN_BUILD%\physx-native.dll %WIN_PACKAGE%\physx-native.dll >nul
if errorlevel 1 goto BUILD_FAILED
copy /Y %WIN_BUILD%\physx-native.pdb %WIN_PACKAGE%\physx-native.pdb >nul
if errorlevel 1 goto BUILD_FAILED

for %%F in (
    PhysX_64.dll
    PhysXCommon_64.dll
    PhysXFoundation_64.dll
    PhysXCooking_64.dll
    PhysX_64.pdb
    PhysXCommon_64.pdb
    PhysXFoundation_64.pdb
    PhysXCooking_64.pdb
) do (
    copy /Y %WIN_SDK%\%%F %WIN_PACKAGE%\%%F >nul
    if errorlevel 1 goto BUILD_FAILED
)

rem Convert the generated layouts to the Android arm64 ABI.
echo Generating Android arm64 binding layouts...
powershell -NoProfile -ExecutionPolicy Bypass -File physx-rs\physx-sys\generate-android-bindings.ps1 ^
    -GeneratedDirectory "%PHYSX_CARGO_OUT%" ^
    -OutputDirectory %ANDROID_BINDINGS%
if errorlevel 1 goto BUILD_FAILED

rem Build and package the Android wrapper with the shared PhysX runtime libraries.
echo Building libphysx-native.so...
if not exist %ANDROID_BUILD% md %ANDROID_BUILD%
cmake -S %NATIVE_SOURCE% -B %ANDROID_BUILD% -G Ninja ^
    -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%build\cmake\android.toolchain.cmake ^
    -DANDROID_ABI=%ANDROID_ABI% ^
    -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
    -DANDROID_STL=%ANDROID_STL% ^
    -DCMAKE_BUILD_TYPE=Release ^
    "-DCMAKE_C_FLAGS_RELEASE=%ANDROID_C_FLAGS%" ^
    "-DCMAKE_CXX_FLAGS_RELEASE=%ANDROID_CPP_FLAGS%" ^
    "-DCMAKE_SHARED_LINKER_FLAGS=%ANDROID_LD_FLAGS%" ^
    -DPHYSX_SDK=%PHYSX_PACKAGE% ^
    -DPHYSX_GENERATED_DIR=%ANDROID_BINDINGS%
if errorlevel 1 goto BUILD_FAILED

cmake --build %ANDROID_BUILD%
if errorlevel 1 goto BUILD_FAILED

if not exist %ANDROID_PACKAGE% md %ANDROID_PACKAGE%
copy /Y %ANDROID_BUILD%\libphysx-native.so %ANDROID_PACKAGE%\libphysx-native.so >nul
if errorlevel 1 goto BUILD_FAILED

for %%F in (
    libPhysX_64.so
    libPhysXCommon_64.so
    libPhysXFoundation_64.so
    libPhysXCooking_64.so
) do (
    copy /Y %ANDROID_SDK%\%%F %ANDROID_PACKAGE%\%%F >nul
    if errorlevel 1 goto BUILD_FAILED
)

rem Strip release Android shared libraries after copying them to the final package.
for %%F in (%ANDROID_PACKAGE%\*.so) do (
    %LLVM_STRIP% --strip-unneeded %%~fF
    if errorlevel 1 goto BUILD_FAILED
)

echo Windows and Android arm64 PhysX 5.9 and MagicPhysX build completed successfully.
popd
endlocal
exit /b 0

rem Preserve the failing command's exit code for callers and CI.
:BUILD_FAILED
set BUILD_RESULT=%ERRORLEVEL%
if %BUILD_RESULT% == 0 set BUILD_RESULT=1
echo PhysX 5.9 and MagicPhysX build failed.
popd
endlocal & exit /b %BUILD_RESULT%
