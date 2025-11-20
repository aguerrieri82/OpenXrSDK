call ..\..\..\third-party\vars.cmd

SET NDK_PROJECT_PATH=%CD%

call %NDK_HOME%\build\ndk-build NDK_DEBUG=0

%LLVM_STRIP% --strip-unneeded libs\arm64-v8a\libbullet-native.so

md ..\..\..\libs\bullet3-native\android-arm64\

copy libs\arm64-v8a\*.so ..\..\..\libs\bullet3-native\android-arm64\