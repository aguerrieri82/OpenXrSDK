call vars.cmd

cd openal-soft
md out-android
cd out-android

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	 
ninja install
	 
copy install\lib\libopenal.so ..\..\..\libs\openal\android-arm64
	
cd..
md out-win
cd out-win

call "%VC_HOME%\Auxiliary\Build\vcvars64.bat"

cmake -G Ninja .. ^
	-DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	
ninja install

copy install\bin\OpenAL32.dll ..\..\..\libs\openal\win-x64