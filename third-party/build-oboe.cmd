call vars.cmd

cd oboe
md out
cd out

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=../../../libs/oboe ^
		 -DALSOFT_BACKEND_OBOE=ON ^
		 -DCMAKE_CXX_FLAGS="-g0" ^
		 -DCMAKE_C_FLAGS="-g0" 
	 
ninja install