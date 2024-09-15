call vars.cmd

cd etcpak

SET NDK_PROJECT_PATH=%CD%

call %NDK_HOME%\build\ndk-build NDK_DEBUG=0

copy libs\arm64-v8a\libetcpack.so ..\..\libs\etcpack\android-arm64