#pragma once


typedef float Matrix4x4[16];

typedef int32_t RTID;
typedef uint32_t VIEWID;
typedef std::bitset<128> OBJID;

template <>
struct std::less<OBJID>
{
	bool operator()(const OBJID& a, const OBJID& b) const { return (memcmp(&a, &b, 16) < 0); }
};

enum class ReleaseContextMode {
	NotRelease = 0,
	ReleaseOnExecute = 1,
	ReleaseAndSuspend = 2
};


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

struct Color3 {
	float r;
	float g;
	float b;
};

struct Color4 {
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
	Vector3 max;
	Vector3 min;
};

struct InitializeOptions {
	Backend driver;
	void* windowHandle;
	void* context;
	const char materialCachePath[256];
	bool enableStereo;
	bool oneViewPerTarget;
	bool useSrgb;
};


struct Geometry {
	VertexBuffer* vb;
	IndexBuffer* ib;
	short4* soBuffer;
	Box box;

};

struct LightInfo {
	LightManager::Type type;
	float intensity;
	float falloffRadius;
	Color4 color;
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
	BlendMode blendMode;
	AntiAliasing antiAliasing;
	bool frustumCullingEnabled;
	bool postProcessingEnabled;
	RenderQuality renderQuality;
	uint32_t sampleCount;
	bool screenSpaceRefractionEnabled;
	bool shadowingEnabled;
	bool stencilBufferEnabled;
	ShadowType shadowType;
	Rect viewport;
	RTID renderTargetId;
};

struct RenderTargetOptions {
	intptr_t textureId;
	uint32_t width;
	uint32_t height;
	uint32_t sampleCount;
	filament::Texture::InternalFormat format;
	uint32_t depth;
};

struct CameraEyeInfo {
	Matrix4x4 relTransform;
	Matrix4x4 projection;
};

struct CameraInfo {
	Matrix4x4 transform;
	Matrix4x4 projection;
	float far;
	float near;
	bool isStereo;
	CameraEyeInfo eyes[2];
};

struct RenderTarget {
	VIEWID viewId;
	RTID renderTargetId;
	CameraInfo camera;
	Rect viewport;
};

struct RenderView
{
	View* view;
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
	uint32_t indicesCount;
	uint8_t* vertices;
	uint32_t verticesCount;
	VertexLayout layout;
	Bounds3 bounds;
};

struct ImageData {
	Texture::Format format;
	Texture::Type type;
	uint8_t* data;
	uint32_t dataSize;
	bool autoFree;
};

struct TextureInfo {
	uint32_t width;
	uint32_t height;
	Texture::InternalFormat internalFormat;
	uint32_t levels;
	ImageData data;
	OBJID textureId;	
};


struct ImageLightInfo {
	TextureInfo texture;
	float intensity;
	float rotation;
	bool showSkybox;
};


struct MaterialInfo {
	TextureInfo normalMap;
	TextureInfo aoMap;
	TextureInfo metallicRoughnessMap;
	TextureInfo baseColorMap;
	TextureInfo emissiveMap;
	Color4 baseColorFactor;
	bool clearCoat;
	bool anisotropy;
	bool multiBounceAO;
	bool specularAntiAliasing; //true;
	bool clearCoatIorChange;
	bool doubleSided;
	bool screenSpaceReflection; //True
	BlendingMode blending;
	MaterialBuilder::SpecularAmbientOcclusion specularAO;
	float normalScale;
	float aoStrength;
	float roughnessFactor;
	float metallicFactor;
	float emissiveStrength;
	Color3 emissiveFactor;
	float alphaCutoff;
	float reflectance;
	bool isLit;
	bool writeDepth;
	bool useDepth;
	bool writeColor;
	bool isShadowOnly;
};

struct GraphicContextInfo {
	struct 
	{
		void* glCtx;
		void* hdc;
	} winGL;

	struct  
	{
		VkInstance instance;
		VkDevice device;
		VkPhysicalDevice physicalDevice;
		uint32_t queueFamily;
		uint32_t queue;
	} vulkan;
};


struct FilamentApp {
	Engine* engine;
	Scene* scene;
	Renderer* renderer;
	filament::SwapChain* swapChain;
	Camera* camera;
	std::vector<RenderView> views;
	std::vector<filament::RenderTarget*> renderTargets;
	std::map<OBJID, Texture*> textures;	
	std::map<OBJID, Entity> entities;
	std::map<OBJID, Geometry> geometries;
	std::map<OBJID, MaterialInstance*> materialsInst;
	std::map<std::string, Material*> materials;
	std::string materialCachePath;
	Texture* iblTexture;
	Texture* skyboxTexture;
	IndirectLight* indirectLight;
	Skybox* skybox;

	bool isStereo;
	bool oneViewPerTarget;
};
