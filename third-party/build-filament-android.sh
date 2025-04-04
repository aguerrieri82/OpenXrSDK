export ANDROID_HOME=/usr/lib/android-sdk/
export FILAMENT_PATH=$PWD
export CC=/usr/bin/clang
export CXX=/usr/bin/clang++
export CXXFLAGS=-stdlib=libc++

mkdir -p $FILAMENT_PATH/out/cmake-release-linux

cd $FILAMENT_PATH/out/cmake-release-linux

cmake -G Ninja ../.. \
	-DCMAKE_BUILD_TYPE=Release  \
	-DFILAMENT_SKIP_SAMPLES=ON  \
	-DCMAKE_INSTALL_PREFIX=../release-linux/filament 

ninja install

mkdir -p $FILAMENT_PATH/out/cmake-android-release-aarch64

cd $FILAMENT_PATH/out/cmake-android-release-aarch64


cmake -G Ninja ../.. \
	-DCMAKE_BUILD_TYPE=Release \
	-DFILAMENT_SKIP_SAMPLES=ON \
	-DFILAMENT_SUPPORTS_VULKAN=ON \
	-DFILAMENT_SUPPORTS_OPENGL=ON \
	-DFILAMENT_USE_EXTERNAL_GLES3=OFF \
	-DFILAMENT_ENABLE_MULTIVIEW=ON \
	-DCMAKE_INSTALL_PREFIX=../android-release/filament \
	-DCMAKE_TOOLCHAIN_FILE=../../build/toolchain-aarch64-linux-android.cmake 

ninja install
