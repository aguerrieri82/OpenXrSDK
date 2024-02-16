cmake_minimum_required(VERSION 3.22.1)

project("draco-native")
  
set(SourceRoot "../../..")
set(DracoSdk "${SourceRoot}/../../packages/draco.CPP.1.3.3.1/build/native/")

include_directories(
		${DracoSdk}/include
)

link_directories(
		../libs)

add_library(${PROJECT_NAME} SHARED
	${SourceRoot}/DracoMeshDecoder.h
	${SourceRoot}/Library.h
	${SourceRoot}/DracoMeshDecoder.cpp
	${SourceRoot}/Main.cpp
)

target_link_libraries(${PROJECT_NAME}
		libdracodec.a
)