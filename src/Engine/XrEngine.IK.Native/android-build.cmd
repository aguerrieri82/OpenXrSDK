call ..\..\..\third-party\vars.cmd

SET NDK_PROJECT_PATH=%CD%

call %NDK_HOME%\build\ndk-build NDK_DEBUG=0
if errorlevel 1 exit /b %errorlevel%

%LLVM_STRIP% --strip-unneeded libs\arm64-v8a\libik-native.so
if errorlevel 1 exit /b %errorlevel%

md ..\..\..\libs\ik-native\android-arm64\ 2>nul
copy /Y libs\arm64-v8a\*.so ..\..\..\libs\ik-native\android-arm64\
