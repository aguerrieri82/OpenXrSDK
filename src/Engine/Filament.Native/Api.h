#pragma once


extern "C" {

	EXPORT FilamentApp* APIENTRY Initialize(const InitializeOptions& options);

	EXPORT VIEWID APIENTRY AddView(FilamentApp* app, const ViewOptions& options);

	EXPORT void APIENTRY UpdateView(FilamentApp* app, VIEWID viewId, const ViewOptions& options);

	EXPORT RTID APIENTRY AddRenderTarget(FilamentApp* app, const RenderTargetOptions& options);

	EXPORT void APIENTRY Render(FilamentApp* app, const ::RenderTarget options[], uint32_t count, bool wait);

	EXPORT void APIENTRY AddLight(FilamentApp* app, OBJID id, const LightInfo& info);

	EXPORT void APIENTRY AddImageLight(FilamentApp* app, const ImageLightInfo& info);

	EXPORT void APIENTRY UpdateImageLight(FilamentApp* app, const ImageLightInfo& info);

	EXPORT void APIENTRY AddGeometry(FilamentApp* app, OBJID id, const GeometryInfo& info);

	EXPORT void APIENTRY AddMesh(FilamentApp* app, OBJID id, const MeshInfo& info);

	EXPORT void APIENTRY AddGroup(FilamentApp* app, OBJID id);

	EXPORT void APIENTRY SetObjVisible(FilamentApp* app, const OBJID id, const bool visible);

	EXPORT void APIENTRY SetObjTransform(FilamentApp* app, OBJID id, const Matrix4x4 matrix);

	EXPORT void APIENTRY SetObjParent(FilamentApp* app, OBJID id, OBJID parentId);

	EXPORT void APIENTRY AddMaterial(FilamentApp* app, OBJID id, const ::MaterialInfo& info) noexcept(false);

	EXPORT void APIENTRY UpdateMaterial(FilamentApp* app, OBJID id, const ::MaterialInfo& info);

	EXPORT bool APIENTRY GetGraphicContext(FilamentApp* app, GraphicContextInfo& info);

	EXPORT void APIENTRY ReleaseContext(FilamentApp* app, ReleaseContextMode release);

	EXPORT bool APIENTRY UpdateTexture(FilamentApp* app, OBJID textId, const ImageData& data);

	EXPORT void APIENTRY SetMeshMaterial(FilamentApp* app, const OBJID id, const OBJID matId);

	EXPORT void APIENTRY UpdateMeshGeometry(FilamentApp* app, OBJID meshId, OBJID geometryId, const GeometryInfo& info);

	EXPORT uint8_t* APIENTRY Allocate(size_t size);
}