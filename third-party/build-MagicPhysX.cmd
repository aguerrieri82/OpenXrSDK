call vars.cmd


SET CUR_DIR=%CD%

cd physx-rs\physx-sys\src


REM Android

md out-android
cd out-android

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
		 -DANDROID=1

ninja

%LLVM_STRIP% --strip-unneeded libphysxnative.so

copy libphysxnative.so %CUR_DIR%\..\libs\physxnative\android-arm64\libphysxnative.so

cd ..


REM Windows

md out-win64
cd out-win64

cmake -G Ninja .. ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%

ninja

copy physxnative.dll %CUR_DIR%\..\libs\physxnative\win-x64\physxnative.dll

cd %CUR_DIR%