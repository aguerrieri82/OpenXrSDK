call vars.cmd

call "%VC_HOME%\Auxiliary\Build\vcvars64.bat"

SET OUT_PATH=%CD%\..\libs

cd libtiff

SET LIB_PATH=%CD%
SET ZSTD_INCLUDE_DIR=..\..\zlib
SET ZGLUT_INCLUDE_DIR=..\..\glut

md %LIB_PATH%\out-win

cd %LIB_PATH%\out-win
cmake -G Ninja ..\ ^
    -DCMAKE_BUILD_TYPE=Debug ^
	-DZSTD_INCLUDE_DIR=..\..\zlib
	
ninja

copy libtiff\tiff.dll ..\..\..\libs\libtiff\win-x64
