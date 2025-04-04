if "%XR_BUILD_VARS%" == "1" exit /b

SET NDK_HOME=c:\Android\ndk\27.0.12077973\
SET VC_HOME=C:\Program Files\Microsoft Visual Studio\2022\Preview\VC

SET ANDROID_ABI=arm64-v8a
SET ANDROID_PLATFORM=30
SET ANDROID_STL=c++_static
SET ANDROID_CPP_FLAGS=-fexceptions -frtti
SET ANDROID_LD_FLAGS="-Wl,-z,max-page-size=16384"
SET LLVM_STRIP="%NDK_HOME%toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-strip"

SET BUILD_TYPE=release
SET INSTALL_PEFIX=install

SET ASM_NASM=C:/msys64/usr/bin/nasm.exe

SET XR_BUILD_VARS=1

call "%VC_HOME%\Auxiliary\Build\vcvars64.bat"