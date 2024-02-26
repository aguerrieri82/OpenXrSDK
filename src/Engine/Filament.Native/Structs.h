#pragma once

typedef float Matrix4x4[16];

typedef int32_t RTID;
typedef uint32_t VIEWID;
typedef uint32_t OBJID;


enum class LightType {
	Sun,           
	Directional,   
	Point,       
	FocusedSpot,  
	Spot,         
};

enum class GraphicDriver {
	Auto = 0,
	OpenGL = 1,
	Vulkan = 2
};

enum class VertexAttributeType {
	Position,
	Normal,
	Tangent,
	Color,
	UV0,
	UV1
};

struct Color {
	float r;
	float g;
	float b;
	float a;
};

struct Rect {
	uint32_t x;
	uint32_t y;
	uint32_t width;
	uint32_t height;

};

struct Vector3 {
	float x;
	float y;
	float z;
};

struct Bounds3 {
	Vector3 min;
	Vector3 max;
};

struct InitializeOptions {
	Backend driver;
	void* windowHandle;
	void* context;
};


struct Geometry {
	VertexBuffer* vb;
	IndexBuffer* ib;
	quatf* soBuffer;
	Box box;

};

struct FilamentApp {
	Engine* engine;
	Scene* scene;
	Renderer* renderer;
	SwapChain* swapChain;
	Camera* camera;
	std::vector<View*> views;
	std::vector<RenderTarget*> renderTargets;
	std::map<OBJID, Entity> entities;
	std::map<OBJID, Geometry> geometries;
	std::map<OBJID, MaterialInstance*> materials;
};

struct LightInfo {
	LightManager::Type type;
	float intensity;
	float falloffRadius;
	::Color color;
	Vector3 direction;
	Vector3 position;
	bool castShadows;
	bool castLight;
	struct {
		float angularRadius;
		float haloFalloff;
		float haloSize;
	} sun;
};

struct ViewOptions {

};

struct RenderTargetOptions {
	intptr_t textureId;
	uint32_t width;
	uint32_t height;
	uint32_t sampleCount;
};

struct CameraInfo {
	Matrix4x4 transform;
	Matrix4x4 projection;
	float far;
	float near;
};

struct RenderInfo {
	VIEWID viewId;
	RTID renderTargetId;
	CameraInfo camera;
	Rect viewport;
};


struct MeshInfo {
	OBJID geometryId;
	OBJID materialId;
	bool culling;
	bool castShadows;
	bool receiveShadows;
	bool fog;
};

struct VertexAttribute {
	VertexAttributeType type;
	uint32_t offset;
	uint32_t size;
};

struct VertexLayout {
	uint32_t sizeByte;
	::VertexAttribute* attributes;
	uint32_t attributeCount;

};

struct GeometryInfo {
	uint32_t* indices;
	size_t indicesCount;
	uint8_t* vertices;
	size_t verticesCount;
	VertexLayout layout;
	Bounds3 bounds;
};

struct ImageData {
	Texture::Format format;
	Texture::Type type;
	uint8_t* data;
	uint32_t dataSize;
};

struct TextureInfo {
	uint32_t width;
	uint32_t height;
	Texture::InternalFormat internalFormat;
	uint32_t levels;
	ImageData data;
};


struct MaterialInfo {
	TextureInfo normalMap;
	TextureInfo aoMap;
	TextureInfo metallicRoughnessMap;
	TextureInfo baseColorMap;
	::Color color;
	bool clearCoat;
	bool anisotropy;
	bool multiBounceAO;
	bool specularAntiAliasing; //true;
	bool clearCoatIorChange;
	bool doubleSided;
	bool screenSpaceReflection; //True
	BlendingMode blending;
	MaterialBuilder::SpecularAmbientOcclusion specularAO;

};