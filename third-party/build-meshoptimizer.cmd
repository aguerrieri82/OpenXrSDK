call vars.cmd

cd meshoptimizer
md out-android
cd out-android

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DMESHOPT_BUILD_SHARED_LIBS=ON ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	 
ninja install

%LLVM_STRIP% --strip-unneeded install\lib\libmeshoptimizer.so

copy install\lib\libmeshoptimizer.so ..\..\..\libs\meshoptimizer\android-arm64\libmeshoptimizer-native.so
	 
cd..
md out-win
cd out-win

del CMakeCache.txt

cmake -G Ninja .. ^
	-DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
	-DMESHOPT_BUILD_SHARED_LIBS=ON ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	
ninja install


copy install\bin\meshoptimizer.dll ..\..\..\libs\meshoptimizer\win-x64\meshoptimizer-native.dll
