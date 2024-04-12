#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <fstream>
#include <bitset>

#define BACKEND_OPENGL_LEVEL_GLES31

#ifdef _WINDOWS

#define WIN32_LEAN_AND_MEAN             

#include <windows.h>
#include <gl/gl.h>
#include <backend/platforms/PlatformWGL.h>

#else

int fopen_s(FILE** _Stream, char const* _FileName, char const* _Mode) {
	
	*_Stream = fopen(_FileName, _Mode);
	return *_Stream != nullptr ? 0 : 1;
}

#endif

#ifdef __ANDROID__
#include <backend/platforms/PlatformEGLAndroid.h>
#include <backend/platforms/PlatformEGLHeadless.h>
#endif


#include <filesystem>
#include <map>

#include <filament/Engine.h>
#include <filament/Texture.h>
#include <filament/Scene.h>
#include <filament/RenderTarget.h>
#include <filament/RenderableManager.h>
#include <filament/Renderer.h>
#include <filament/View.h>
#include <filament/Camera.h>
#include <filament/SwapChain.h>
#include <filament/IndirectLight.h>
#include <filament/LightManager.h>
#include <filament/TransformManager.h>
#include <filament/IndexBuffer.h>
#include <filament/VertexBuffer.h>
#include <filament/BufferObject.h>
#include <filament/Material.h>
#include <filament/TextureSampler.h>
#include <filament/Viewport.h>
#include <filament/IndirectLight.h>
#include <filament/Skybox.h>

#include <filamat/MaterialBuilder.h>

#include <geometry/SurfaceOrientation.h>
#include <geometry/TangentSpaceMesh.h>

#include <utils/EntityManager.h>
#include <utils/Log.h>

#include <backend/DriverEnums.h>
#include <backend/Platform.h>
#include <backend/platforms/OpenGLPlatform.h>

#include <backend/platforms/VulkanPlatform.h>


#include <filament-iblprefilter/IBLPrefilterContext.h>

