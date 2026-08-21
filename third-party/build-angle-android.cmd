@echo off
setlocal

set "SCRIPT_SOURCE=%~dp0build-angle.sh"
set "DIST_DIR=%~dp0..\libs\angle\android-arm64"

if not exist "%SCRIPT_SOURCE%" (
    echo ERROR: Missing file:
    echo %SCRIPT_SOURCE%
    exit /b 1
)

wsl.exe -d Ubuntu -e bash -lc "mkdir -p ~/src && cp -f \"$(wslpath '%SCRIPT_SOURCE%')\" ~/src/build-angle.sh && chmod +x ~/src/build-angle.sh && ~/src/build-angle.sh \"$(wslpath '%DIST_DIR%')\"" 2>&1

exit /b %ERRORLEVEL%