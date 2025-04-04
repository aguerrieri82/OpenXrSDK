call vars.cmd

SET CUR_DIR=%CD%

cd openal-soft
md out-android
cd out-android

del CMakeCache.txt

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
		 -DALSOFT_BACKEND_OBOE=ON ^
		 -DCMAKE_CXX_FLAGS="-g0" ^
		 -DCMAKE_C_FLAGS="-g0" ^
		 -DOBOE_SOURCE="%CUR_DIR%\oboe"



ninja install
	 
%LLVM_STRIP% --strip-unneeded install\lib\libopenal.so

copy install\lib\libopenal.so ..\..\..\libs\openal\android-arm64
	
cd..
md out-win
cd out-win

del CMakeCache.txt

cmake -G Ninja .. ^
	-DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	
ninja install

copy install\bin\OpenAL32.dll ..\..\..\libs\openal\win-x64