call vars.cmd

SET CUR_PATH=%CD%

SET BULLET_BASE=D:\Development\Library\bullet3

md %BULLET_BASE%\out\andorid

cd %BULLET_BASE%\out\andorid

cmake -G Ninja %BULLET_BASE%\examples\ThirdPartyLibs\BussIK ^
         -DCMAKE_TOOLCHAIN_FILE=%NDK_HOME%\build\cmake\android.toolchain.cmake ^
	     -DANDROID_ABI=%ANDROID_ABI% ^
	     -DANDROID_PLATFORM=%ANDROID_PLATFORM% ^
	     -DANDROID_STL=%ANDROID_STL% ^
		 -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
		 -DCMAKE_INSTALL_PREFIX=%BULLET_BASE%\out\andorid
		 -DCMAKE_CXX_FLAGS="-g0" ^
		 -DCMAKE_C_FLAGS="-g0" 
	 
ninja install

md "%CUR_PATH%/../libs/bullet3/android-arm64/"

xcopy "%BULLET_BASE%\out\andorid\lib\*.*" "%CUR_PATH%/../libs/bullet3/android-arm64/" /I /Y