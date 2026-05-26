call vars.cmd

SET CUR_DIR=%CD%

cd astc-encoder-native
md out-android
cd out-android

del CMakeCache.txt

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
		 -DASTCENC_TARGET_ARM=ON ^
		 -DANDROID=1



ninja astcencoder-native

%LLVM_STRIP% --strip-unneeded libastcencoder-native.so

copy libastcencoder-native.so ..\..\..\libs\astcencoder-native\android-arm64

cd..
md out-win
cd out-win

del CMakeCache.txt

cmake -G Ninja .. ^
    -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	
ninja astcencoder-native

copy astcencoder-native.* ..\..\..\libs\astcencoder-native\win-x64

cd..
cd..