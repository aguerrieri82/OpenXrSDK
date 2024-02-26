#pragma once


extern "C" {

	EXPORT FilamentApp* APIENTRY Initialize(InitializeOptions& options);

	EXPORT VIEWID APIENTRY AddView(FilamentApp* app, ViewOptions& options);

	EXPORT RTID APIENTRY AddRenderTarget(FilamentApp* app, RenderTargetOptions& options);

	EXPORT void APIENTRY Render(FilamentApp* app, ::RenderTarget options[], uint32_t count);

	EXPORT void APIENTRY AddLight(FilamentApp* app, OBJID id, LightInfo& info);

	EXPORT void APIENTRY AddGeometry(FilamentApp* app, OBJID id, GeometryInfo& info);

	EXPORT void APIENTRY AddMesh(FilamentApp* app, OBJID id, MeshInfo& info);

	EXPORT void APIENTRY AddGroup(FilamentApp* app, OBJID id);

	EXPORT void APIENTRY SetObjTransform(FilamentApp* app, OBJID id, Matrix4x4 matrix);

	EXPORT void APIENTRY SetObjParent(FilamentApp* app, OBJID id, OBJID parentId);

	EXPORT void APIENTRY AddMaterial(FilamentApp* app, OBJID id, ::MaterialInfo & info);

	EXPORT bool APIENTRY GetGraphicContext(FilamentApp* app, GraphicContextInfo& info);

	EXPORT void APIENTRY ReleaseContext(FilamentApp* app, bool release);


}