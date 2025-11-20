call vars.cmd

SET CUR_DIR=%CD%
SET FF3_BASE=D:\Development\Library\fftw3

cd %FF3_BASE%

md out-win
cd out-win

cmake -G Ninja .. ^
	-DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
	-DENABLE_SSE2=ON ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX%
	
ninja install

goto end

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

REM %LLVM_STRIP% --strip-unneeded library\lib\libsfizz_api.so

REM copy library\lib\libsfizz_api.so %CUR_DIR%\..\src\Engine\Sfizz\libs\arm64-v8a\libsfizz_api.so

end:
cd %CUR_DIR%