call vars.cmd

call "%VC_HOME%\Auxiliary\Build\vcvars64.bat"

cd MagicPhysX\src\libmagicphysx

@echo ANDROID BUILD

rustup target add aarch64-linux-android

cargo install cargo-ndk
 
SET ANDROID_NDK_ROOT=%NDK_HOME%
SET NDK_PROJECT_PATH=%CD%

cargo ndk --platform 26 --target aarch64-linux-android build --release 

copy target\aarch64-linux-android\release\libphysxnative.so ..\..\..\..\libs\physxnative\android-arm64

@echo WIN BUILD

rustup default stable

cargo build --release

copy target\release\physxnative.dll ..\..\..\..\libs\physxnative\win-x64
