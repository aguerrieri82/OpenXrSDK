call vars.cmd

SET CUR_DIR=%CD%
SET SFIZZ_BASE=D:\Development\Library\sfizz\

cd %SFIZZ_BASE%
md out-android
cd out-android

cmake -G Ninja .. -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%INSTALL_PEFIX% ^
		 -DSFIZZ_USE_SYSTEM_SIMDE=OFF ^
		 -DANDROID=1
	  
ninja sfizz_api

%LLVM_STRIP% --strip-unneeded library\lib\libsfizz_api.so

copy library\lib\libsfizz_api.so %CUR_DIR%\..\src\Engine\Sfizz\libs\arm64-v8a\libsfizz_api.so

cd %CUR_DIR%