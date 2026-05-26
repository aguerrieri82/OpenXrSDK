call vars.cmd

SET CUR_DIR=%CD%


cd oboe
md out 
cd out

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DOBOE_NO_INCLUDE_AAUDIO=ON ^
		 -DCMAKE_INSTALL_PREFIX=../../../libs/oboe ^
		 -DALSOFT_BACKEND_OBOE=ON ^
		 -DCMAKE_CXX_FLAGS="-g0" ^
		 -DCMAKE_C_FLAGS="-g0" 
	 
ninja install

cd %CUR_DIR%