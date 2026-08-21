@echo off
setlocal EnableExtensions

set "ROOT=%~dp0google"
set "ANGLE_DIR=%ROOT%\angle"
set "DEPOT_TOOLS_DIR=%ROOT%\depot_tools"
set "OUT_DIR=out\win-x64"
set "OUT_PATH=%ANGLE_DIR%\%OUT_DIR%"
set "DIST_DIR=%~dp0..\libs\angle\win-x64"

set "ANGLE_REPO=https://github.com/aguerrieri82/angle.git"
set "ANGLE_BRANCH=%~1"
if not defined ANGLE_BRANCH set "ANGLE_BRANCH=main"

set "DEPOT_TOOLS_WIN_TOOLCHAIN=0"
set "GCLIENT_SUPPRESS_GIT_VERSION_WARNING=1"

rem
rem depot_tools
rem

if exist "%DEPOT_TOOLS_DIR%\.git" goto update_depot_tools

echo Cloning depot_tools...

git.exe clone ^
    https://chromium.googlesource.com/chromium/tools/depot_tools.git ^
    "%DEPOT_TOOLS_DIR%"

if errorlevel 1 goto error

goto depot_tools_ready

:update_depot_tools
echo Updating depot_tools...

git.exe -C "%DEPOT_TOOLS_DIR%" fetch origin
if errorlevel 1 goto error

git.exe -C "%DEPOT_TOOLS_DIR%" checkout -f origin/main
if errorlevel 1 goto error

:depot_tools_ready

rem
rem ANGLE
rem
rem Do this before adding depot_tools to PATH, otherwise depot_tools\git.bat
rem can terminate this script when invoked without CALL.
rem

if exist "%ANGLE_DIR%\.git" goto fetch_angle

echo Cloning ANGLE...

git.exe clone ^
    --branch "%ANGLE_BRANCH%" ^
    "%ANGLE_REPO%" ^
    "%ANGLE_DIR%"

if errorlevel 1 goto error

goto angle_ready

:fetch_angle
echo Fetching ANGLE...

git.exe -C "%ANGLE_DIR%" fetch origin
if errorlevel 1 goto error

:angle_ready
echo ANGLE ready.

rem
rem depot_tools commands are needed from this point onward.
rem

set "PATH=%DEPOT_TOOLS_DIR%;%PATH%"

rem
rem Create .gclient if missing
rem

if exist "%ROOT%\.gclient" goto gclient_ready

echo Creating .gclient...

>"%ROOT%\.gclient" echo solutions = [
>>"%ROOT%\.gclient" echo   {
>>"%ROOT%\.gclient" echo     "name": "angle",
>>"%ROOT%\.gclient" echo     "url": "%ANGLE_REPO%",
>>"%ROOT%\.gclient" echo     "deps_file": "DEPS",
>>"%ROOT%\.gclient" echo     "managed": False,
>>"%ROOT%\.gclient" echo     "custom_deps": {},
>>"%ROOT%\.gclient" echo     "custom_vars": {
>>"%ROOT%\.gclient" echo       "checkout_angle_cl_deps": False,
>>"%ROOT%\.gclient" echo     },
>>"%ROOT%\.gclient" echo   },
>>"%ROOT%\.gclient" echo ]

:gclient_ready

rem
rem Dependency sync
rem
rem First sync requires the complete initialization flags.
rem Later syncs are incremental.
rem

pushd "%ROOT%"
if errorlevel 1 goto error

if exist "%ROOT%\.gclient_entries" goto incremental_sync

echo Initial dependency sync...

call gclient sync -f -D -R --no-history --shallow
if errorlevel 1 goto error_popd

goto sync_complete

:incremental_sync
echo Incremental dependency sync...

call gclient sync
if errorlevel 1 goto error_popd

:sync_complete
popd

rem
rem GN configuration
rem

set GN_ARGS=target_os=""win"" ^
 target_cpu=""x64"" ^
 angle_build_all=false ^
 is_debug=false ^
 is_component_build=false ^
 use_custom_libcxx=true ^
 treat_warnings_as_errors=false ^
 angle_has_frame_capture=false ^
 angle_enable_gl=false ^
 angle_enable_vulkan_validation_layers=true ^
 angle_enable_vulkan=true ^
 angle_enable_wgpu=false ^
 angle_enable_d3d11=false ^
 angle_enable_null=false ^
 angle_debug_layers_enabled=true ^
 use_siso=false

rem
rem Generate and build incrementally
rem

pushd "%ANGLE_DIR%"
if errorlevel 1 goto error

echo Generating build...

call gn gen "%OUT_DIR%" --args="%GN_ARGS%"
if errorlevel 1 goto error_popd

echo Building ANGLE...

call autoninja --offline -C "%OUT_DIR%" libEGL libGLESv2
if errorlevel 1 goto error_popd

popd

rem
rem Copy runtime output
rem

if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"
if errorlevel 1 goto error

if not exist "%DIST_DIR%\angledata" mkdir "%DIST_DIR%\angledata"
if errorlevel 1 goto error

robocopy "%OUT_PATH%" "%DIST_DIR%" ^
    libEGL.dll ^
    libGLESv2.dll ^
    libEGL.dll.pdb ^
    libGLESv2.dll.pdb ^
    vulkan-1.dll ^
    VkLayer_khronos_validation.dll ^
    /R:1 /W:1 /NFL /NDL /NJH /NJS /NP

if errorlevel 8 goto copy_error

robocopy "%OUT_PATH%\angledata" "%DIST_DIR%\angledata" ^
    /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP

if errorlevel 8 goto copy_error

echo.
echo ANGLE Windows x64 build complete:
echo %DIST_DIR%
exit /b 0

:error_popd
set "ERR=%ERRORLEVEL%"
popd
goto failed

:copy_error
set "ERR=%ERRORLEVEL%"
echo.
echo COPY FAILED - robocopy errorlevel %ERR%
exit /b %ERR%

:error
set "ERR=%ERRORLEVEL%"
if "%ERR%"=="0" set "ERR=1"

:failed
echo.
echo BUILD FAILED - errorlevel %ERR%
exit /b %ERR%