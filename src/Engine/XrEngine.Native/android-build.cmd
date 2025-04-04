call ..\..\..\third-party\vars.cmd

SET NDK_PROJECT_PATH=%CD%

call %NDK_HOME%\build\ndk-build NDK_DEBUG=0

%LLVM_STRIP% --strip-unneeded libs\arm64-v8a\libxrengine-native.so

copy libs\arm64-v8a\*.so ..\..\..\libs\xrengine-native\android-arm64\