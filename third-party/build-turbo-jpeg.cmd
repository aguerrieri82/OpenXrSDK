call vars.cmd

cd libjpeg-turbo
md out-android
cd out-android

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
		 -DANDROID=1
	  
ninja install

%LLVM_STRIP% --strip-unneeded install\lib\libturbojpeg.so
	 
copy install\lib\libturbojpeg.so ..\..\..\libs\turbo-jpeg\android-arm64\libturbojpeg-native.so

cd..
md out-win
cd out-win

del CMakeCache.txt
 
cmake -G Ninja .. ^
	 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
	 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%

ninja install

copy install\bin\turbojpeg.dll  ..\..\..\libs\turbo-jpeg\win-x64\turbojpeg-native.dll