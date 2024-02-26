#pragma once

#ifdef _WINDOWS

#define WIN32_LEAN_AND_MEAN             

#include <windows.h>

#endif

#include <map>
#include <gl/gl.h>

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
#include <filament/Material.h>
#include <filament/TextureSampler.h>
#include <filament/Viewport.h>

#include <filamat/MaterialBuilder.h>



#include <geometry/SurfaceOrientation.h>

#include <utils/EntityManager.h>


#include <backend/DriverEnums.h>
#include <backend/Platform.h>
#include <backend/platforms/PlatformWGL.h>
#include <backend/platforms/OpenGLPlatform.h>