SET FILAMENT_PATH=D:\Development\Personal\Git\XrSDK\third-party\filament
cd %FILAMENT_PATH%\out\cmake-release-android
 ninja install
 xcopy %FILAMENT_PATH%\out\android-release\filament\include %OUT_PATH%\filament-android\include /S /Y
 xcopy %FILAMENT_PATH%\out\android-release\filament\lib\arm64-v8a %OUT_PATH%\filament-android\lib\arm64-v8a /S /Y
cd D:\Development\Personal\Git\XrSDK\src\Engine\Filament.Native
call android-build.cmd
cd D:\Development\Personal\Git\XrSDK\third-party\