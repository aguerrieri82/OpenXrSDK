#pragma once


extern "C" {

	EXPORT FilamentApp* APIENTRY Initialize(const InitializeOptions& options);

	EXPORT VIEWID APIENTRY AddView(FilamentApp* app, const ViewOptions& options);

	EXPORT RTID APIENTRY AddRenderTarget(FilamentApp* app, const RenderTargetOptions& options);

	EXPORT void APIENTRY Render(FilamentApp* app, const ::RenderTarget options[], uint32_t count, bool wait);

	EXPORT void APIENTRY AddLight(FilamentApp* app, OBJID id, const LightInfo& info);

	EXPORT void APIENTRY AddGeometry(FilamentApp* app, OBJID id, const GeometryInfo& info);

	EXPORT void APIENTRY AddMesh(FilamentApp* app, OBJID id, const MeshInfo& info);

	EXPORT void APIENTRY AddGroup(FilamentApp* app, OBJID id);

	EXPORT void APIENTRY SetObjTransform(FilamentApp* app, OBJID id, const Matrix4x4 matrix);

	EXPORT void APIENTRY SetObjParent(FilamentApp* app, OBJID id, OBJID parentId);

	EXPORT void APIENTRY AddMaterial(FilamentApp* app, OBJID id, const ::MaterialInfo& info) noexcept(false);

	EXPORT bool APIENTRY GetGraphicContext(FilamentApp* app, GraphicContextInfo& info);

	EXPORT void APIENTRY ReleaseContext(FilamentApp* app, ReleaseContextMode release);


}