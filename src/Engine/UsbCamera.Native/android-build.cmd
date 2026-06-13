call ..\..\..\third-party\vars.cmd

SET NDK_PROJECT_PATH=%CD%

SET BUILD_TYPE=release

REM call %NDK_HOME%\build\ndk-build NDK_DEBUG=1

call %NDK_HOME%\build\ndk-build NDK_DEBUG=0

%LLVM_STRIP% --strip-unneeded libs\arm64-v8a\libusbcamera-native.so

mkdir ..\..\..\libs\usbcamera-native\android-arm64\

copy libs\arm64-v8a\*.so ..\..\..\libs\usbcamera-native\android-arm64\

copy obj\local\arm64-v8a\libusbcamera-native.so ..\..\..\libs\usbcamera-native\android-arm64\