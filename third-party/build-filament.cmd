call vars.cmd

call "%VC_HOME%\Auxiliary\Build\vcvars64.bat"

SET OUT_PATH=%CD%\..\libs

cd filament

SET FILAMENT_PATH=%CD%

cd %FILAMENT_PATH%\out\cmake-release-win
cmake -G Ninja ..\.. ^
    -DCMAKE_INSTALL_PREFIX=..\release-win\filament ^
    -DFILAMENT_ENABLE_JAVA=NO ^
	-DFILAMENT_SKIP_SAMPLES=ON ^
	-DFILAMENT_ENABLE_MULTIVIEW=ON ^
    -DCMAKE_BUILD_TYPE=Release ^
	-DFILAMENT_SUPPORTS_VULKAN=ON ^
	-DFILAMENT_SUPPORTS_OPENGL=ON
	
ninja install


xcopy %FILAMENT_PATH%\out\release-win\filament\include %OUT_PATH%\filament-windows\include /S /Y
xcopy %FILAMENT_PATH%\out\release-win\filament\lib\x86_64 %OUT_PATH%\filament-windows\lib\x86_64\mt /S /Y


cd %FILAMENT_PATH%\out\cmake-debug-win
cmake -G Ninja ..\.. ^
    -DCMAKE_INSTALL_PREFIX=..\debug-win\filament ^
    -DFILAMENT_ENABLE_JAVA=NO ^
	-DFILAMENT_SKIP_SAMPLES=ON ^
    -DCMAKE_BUILD_TYPE=Debug ^
	-DFILAMENT_ENABLE_MULTIVIEW=ON ^
	-DFILAMENT_SUPPORTS_VULKAN=ON ^
	-DFILAMENT_SUPPORTS_OPENGL=ON
    

ninja install

xcopy %FILAMENT_PATH%\out\debug-win\filament\include %OUT_PATH%\filament-windows\include /S /Y
xcopy %FILAMENT_PATH%\out\debug-win\filament\lib\x86_64 %OUT_PATH%\filament-windows\lib\x86_64\mtd /S /Y

cd %FILAMENT_PATH%

bash -c ../android-build.sh

xcopy %FILAMENT_PATH%\out\android-release\filament\include %OUT_PATH%\filament-android\include /S /Y
xcopy %FILAMENT_PATH%\out\android-release\filament\lib\arm64-v8a %OUT_PATH%\filament-android\lib\arm64-v8a /S /Y

