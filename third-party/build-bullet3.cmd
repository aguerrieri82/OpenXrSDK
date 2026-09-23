@echo off

call vars.cmd

set "CUR_PATH=%CD%"

set "BULLET_BASE=D:\Development\Library\bullet3"
set "BUSSIK_SRC=%BULLET_BASE%\examples\ThirdPartyLibs\BussIK"


REM ============================================================
REM Android
REM ============================================================

set "ANDROID_BUILD_DIR=%BULLET_BASE%\out\android"

if not exist "%ANDROID_BUILD_DIR%" md "%ANDROID_BUILD_DIR%"

cmake -S "%BUSSIK_SRC%" -B "%ANDROID_BUILD_DIR%" -G Ninja ^
    -DCMAKE_TOOLCHAIN_FILE="%NDK_HOME%\build\cmake\android.toolchain.cmake" ^
    -DANDROID_ABI=%ANDROID_ABI% ^
    -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
    -DANDROID_STL=%ANDROID_STL% ^
    -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX="%ANDROID_BUILD_DIR%" ^
    -DCMAKE_CXX_FLAGS="-g0" ^
    -DCMAKE_C_FLAGS="-g0"

if errorlevel 1 exit /b %errorlevel%

cmake --build "%ANDROID_BUILD_DIR%" --target install

if errorlevel 1 exit /b %errorlevel%

set "ANDROID_LIB_DST=%CUR_PATH%\..\libs\bullet3\android-arm64"

if not exist "%ANDROID_LIB_DST%" md "%ANDROID_LIB_DST%"

xcopy "%ANDROID_BUILD_DIR%\lib\*.*" "%ANDROID_LIB_DST%\" /I /Y

if errorlevel 1 exit /b %errorlevel%


REM ============================================================
REM Windows
REM ============================================================

if /I "%BUILD_TYPE%"=="Debug" (
    set "WIN_BUILD_TYPE=Debug"
    set "WIN_BUILD_DIR=%BULLET_BASE%\out\build\BussIK\x64-Debug"
    set "WIN_INSTALL_DIR=%BULLET_BASE%\out\install\x64-Debug"
) else (
    set "WIN_BUILD_TYPE=RelWithDebInfo"
    set "WIN_BUILD_DIR=%BULLET_BASE%\out\build\BussIK\x64-Release"
    set "WIN_INSTALL_DIR=%BULLET_BASE%\out\install\x64-Release"
)

if not exist "%WIN_BUILD_DIR%" md "%WIN_BUILD_DIR%"
if not exist "%WIN_INSTALL_DIR%" md "%WIN_INSTALL_DIR%"

cmake -S "%BUSSIK_SRC%" -B "%WIN_BUILD_DIR%" -G Ninja ^
    -DCMAKE_BUILD_TYPE=%WIN_BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX="%WIN_INSTALL_DIR%"

if errorlevel 1 exit /b %errorlevel%

cmake --build "%WIN_BUILD_DIR%" --target install

if errorlevel 1 exit /b %errorlevel%


REM ============================================================
REM BussIK headers
REM ============================================================

set "BUSSIK_INC_DIR=%WIN_INSTALL_DIR%\include\bullet\ThirdPartyLibs\BussIK"

if not exist "%BUSSIK_INC_DIR%" md "%BUSSIK_INC_DIR%"

xcopy "%BUSSIK_SRC%\*.h" "%BUSSIK_INC_DIR%\" /S /I /Y

if errorlevel 1 exit /b %errorlevel%

xcopy "%BUSSIK_SRC%\*.hpp" "%BUSSIK_INC_DIR%\" /S /I /Y

if errorlevel 2 exit /b %errorlevel%


echo.
echo BussIK build completed.
echo Android: %ANDROID_BUILD_DIR%
echo Windows: %WIN_INSTALL_DIR%