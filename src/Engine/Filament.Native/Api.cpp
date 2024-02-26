#include "pch.h"

bool isReleaseContext = false;

class PlatformWGL2 : public PlatformWGL {
protected:
	void commit(SwapChain* swapChain) noexcept override {
		PlatformWGL::commit(swapChain);
		if (isReleaseContext)
			wglMakeCurrent(mWhdc, NULL);
	}
};



mat4 MatFromArray(Matrix4x4 array) {
	return mat4(array[0], array[1], array[2], array[3],
		array[4], array[5], array[6], array[7],
		array[8], array[9], array[10], array[11],
		array[12], array[13], array[14], array[15]);
}


FilamentApp* Initialize(InitializeOptions& options) {

	auto app = new FilamentApp();

	app->materialCachePath = options.materialCachePath;


	app->engine = Engine::Builder()
		.backend((Backend)options.driver)
		.platform(new PlatformWGL2())
		.sharedContext(options.context)
		.build();

	app->scene = app->engine->createScene();
	app->renderer = app->engine->createRenderer();
	app->camera = app->engine->createCamera(EntityManager::get().create());
	if (options.windowHandle != nullptr)
		app->swapChain = app->engine->createSwapChain(options.windowHandle);
	else
		app->swapChain = app->engine->createSwapChain(800, 600);

	return app;
}


VIEWID AddView(FilamentApp* app, ViewOptions& options)
{

	auto view = app->engine->createView();
	view->setBlendMode(options.blendMode);
	view->setAntiAliasing(options.antiAliasing);
	view->setFrustumCullingEnabled(options.frustumCullingEnabled);
	view->setPostProcessingEnabled(options.postProcessingEnabled);
	view->setRenderQuality(options.renderQuality);
	view->setSampleCount(options.sampleCount);
	view->setScreenSpaceRefractionEnabled(options.screenSpaceRefractionEnabled);
	view->setScreenSpaceRefractionEnabled(options.screenSpaceRefractionEnabled);
	view->setShadowingEnabled(options.shadowingEnabled);
	view->setShadowType(options.shadowType);
	view->setStencilBufferEnabled(options.stencilBufferEnabled);


	view->setScene(app->scene);
	app->views.push_back(view);
	return app->views.size() - 1;
}

RTID AddRenderTarget(FilamentApp* app, RenderTargetOptions& options)
{
	auto color = Texture::Builder()
		.width(options.width)
		.height(options.height)
		.levels(1)
		.usage(filament::Texture::Usage::COLOR_ATTACHMENT | filament::Texture::Usage::SAMPLEABLE)
		.format(filament::Texture::InternalFormat::RGBA8)
		.import(options.textureId)
		.build(*app->engine);

	auto depth = Texture::Builder()
		.width(options.width)
		.height(options.height)
		.levels(1)
		.usage(filament::Texture::Usage::DEPTH_ATTACHMENT)
		.format(filament::Texture::InternalFormat::DEPTH24)
		.build(*app->engine);

	auto rt = filament::RenderTarget::Builder()
		.texture(filament::RenderTarget::AttachmentPoint::COLOR, color)
		.texture(filament::RenderTarget::AttachmentPoint::DEPTH, depth)
		.build(*app->engine);

	app->renderTargets.push_back(rt);

	return app->renderTargets.size() - 1;
}


void ReleaseContext(FilamentApp* app, bool release) 
{
	isReleaseContext = release;
	app->renderer->beginFrame(app->swapChain);
	app->renderer->endFrame();
	app->engine->flushAndWait();
}

void Render(FilamentApp* app, ::RenderTarget targets[], uint32_t count)
{
	isReleaseContext = false;

	int iPrev = _CrtSetReportMode(_CRT_ASSERT, 0);

	Renderer::ClearOptions opt;
	opt.clear = true;
	opt.clearColor = { 0, 1, 1, 1 };

	app->renderer->setClearOptions(opt);

	app->renderer->beginFrame(app->swapChain);

	auto lcount = app->scene->getLightCount();

	for (auto i = 0; i < count; i++) {

		auto target = targets[i];
		auto view = app->views[target.viewId];

		view->setDynamicLightingOptions(0.1, 40);

		app->camera->setCustomProjection(MatFromArray(target.camera.projection), target.camera.near, target.camera.far);
		app->camera->setModelMatrix(MatFromArray(target.camera.transform));

		view->setViewport(filament::Viewport(target.viewport.x, target.viewport.y, target.viewport.width, target.viewport.height));
		view->setCamera(app->camera);

		if (target.renderTargetId != -1)
			view->setRenderTarget(app->renderTargets[target.renderTargetId]);
		else
			view->setRenderTarget(nullptr);

		app->renderer->render(view);
	}
	app->renderer->endFrame();
	app->engine->flushAndWait();
}

void AddLight(FilamentApp* app, OBJID id, LightInfo& info)
{
	auto light = EntityManager::get().create();
	LightManager::Builder(info.type)
		.color({ info.color.r ,info.color.g, info.color.b })
		.intensity(info.intensity * 100000)
		.direction({ info.direction.x, info.direction.y, info.direction.z })
		.sunAngularRadius(info.sun.angularRadius)
		.sunHaloFalloff(info.sun.haloFalloff)
		.sunHaloSize(info.sun.haloSize)
		.castShadows(info.castShadows)
		.position({ info.position.x, info.position.y, info.position.z })
		.falloff(info.falloffRadius)
		.castLight(info.castLight)
		.build(*app->engine, light);

	app->scene->addEntity(light);
	app->entities[id] = light;
}

void AddGeometry(FilamentApp* app, OBJID id, GeometryInfo& info)
{
	auto ib = IndexBuffer::Builder()
		.indexCount(info.indicesCount)
		.bufferType(IndexBuffer::IndexType::UINT)
		.build(*app->engine);

	auto ibSize = sizeof(uint32_t) * info.indicesCount;
	auto ibBuffer = new uint8_t[ibSize];
	memcpy(ibBuffer, info.indices, ibSize);

	ib->setBuffer(*app->engine, IndexBuffer::BufferDescriptor(ibBuffer, ibSize, 0));

	auto vbBuilder = VertexBuffer::Builder()
		.vertexCount(info.verticesCount);

	auto surfBuilder = geometry::SurfaceOrientation::Builder();
	surfBuilder
		.triangles((uint3*)info.indices)
		.triangleCount(info.indicesCount / 3)
		.vertexCount(info.verticesCount);

	bool hasOrientation = false;

	Geometry result;

	for (auto i = 0; i < info.layout.attributeCount; i++) {
		auto& attr = info.layout.attributes[i];

		filament::VertexAttribute va;
		VertexBuffer::AttributeType at;

		bool isMainBuffer = true;

		switch (attr.type)
		{
		case VertexAttributeType::Position:
			va = filament::VertexAttribute::POSITION;
			at = VertexBuffer::AttributeType::FLOAT3;
			surfBuilder.positions((float3*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		case VertexAttributeType::Normal:
			surfBuilder.normals((float3*)(info.vertices + attr.offset), info.layout.sizeByte);
			hasOrientation = true;
			isMainBuffer = false;
			break;
		case VertexAttributeType::Tangent:
			surfBuilder.tangents((float4*)(info.vertices + attr.offset), info.layout.sizeByte);
			hasOrientation = true;
			isMainBuffer = false;
			break;
		case VertexAttributeType::Color:
			va = filament::VertexAttribute::COLOR;
			at = VertexBuffer::AttributeType::FLOAT4;
			break;
		case VertexAttributeType::UV0:
			va = filament::VertexAttribute::UV0;
			at = VertexBuffer::AttributeType::FLOAT2;
			surfBuilder.uvs((float2*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		case VertexAttributeType::UV1:
			va = filament::VertexAttribute::UV1;
			at = VertexBuffer::AttributeType::FLOAT2;
			break;
		default:
			break;
		}

		if (isMainBuffer)
			vbBuilder.attribute(va, 0, at, attr.offset, info.layout.sizeByte);
	};

	hasOrientation = false;


	vbBuilder.bufferCount(hasOrientation ? 2 : 1);

	if (hasOrientation)
		vbBuilder.attribute(filament::VertexAttribute::TANGENTS, 1, VertexBuffer::AttributeType::FLOAT4);

	auto vb = vbBuilder.build(*app->engine);

	auto vbSize = info.verticesCount * info.layout.sizeByte;
	auto vbBuffer = new uint8_t[vbSize];
	memcpy(vbBuffer, info.vertices, vbSize);

	vb->setBufferAt(*app->engine, 0, VertexBuffer::BufferDescriptor(vbBuffer, vbSize, 0));

	if (hasOrientation) {

		auto so = surfBuilder.
			build();

		result.soBuffer = new quatf[so->getVertexCount()];
		so->getQuats(result.soBuffer, so->getVertexCount());

		vb->setBufferAt(*app->engine, 1, VertexBuffer::BufferDescriptor(result.soBuffer, so->getVertexCount() * sizeof(quatf), 0));

		delete so;
	}

	float3 halfSize = {
		(info.bounds.max.x - info.bounds.min.x) / 2.0f,
		(info.bounds.max.y - info.bounds.min.y) / 2.0f,
		(info.bounds.max.z - info.bounds.min.z) / 2.0f
	};

	float3 center = {
		(info.bounds.max.x + info.bounds.min.x) / 2.0f,
		(info.bounds.max.y + info.bounds.min.y) / 2.0f,
		(info.bounds.max.z + info.bounds.min.z) / 2.0f
	};

	result.ib = ib;
	result.vb = vb;
	result.box = { center, halfSize };

	app->geometries[id] = result;
}

void AddGroup(FilamentApp* app, OBJID id) {

	auto group = EntityManager::get().create();
	app->scene->addEntity(group);
	app->entities[id] = group;
}

void AddMesh(FilamentApp* app, OBJID id, MeshInfo& info)
{
	auto mesh = EntityManager::get().create();

	auto geo = app->geometries[info.geometryId];
	auto mat = app->materialsInst[info.materialId];

	RenderableManager::Builder(1)
		.boundingBox(geo.box)
		.castShadows(info.castShadows)
		.receiveShadows(info.receiveShadows)
		.material(0, mat)
		.fog(info.fog)
		.geometry(0, PrimitiveType::TRIANGLES, geo.vb, geo.ib)
		.build(*app->engine, mesh);

	app->scene->addEntity(mesh);
	app->entities[id] = mesh;
}

void SetObjParent(FilamentApp* app, OBJID id, OBJID parentId)
{
	auto& tcm = app->engine->getTransformManager();
	auto parentObj = app->entities[parentId];
	auto obj = app->entities[id];
	auto parentInstance = tcm.getInstance(parentObj);
	auto objInstance = tcm.getInstance(obj);
	tcm.setParent(objInstance, parentInstance);
}

Texture* CreateTexture(FilamentApp* app, TextureInfo& info) {

	auto texture = Texture::Builder()
		.width(info.width)
		.height(info.height)
		.levels(info.levels)
		.format(info.internalFormat)
		.build(*app->engine);

	Texture::PixelBufferDescriptor buffer(info.data.data, info.data.dataSize,
		info.data.format, info.data.type);

	texture->setImage(*app->engine, 0, std::move(buffer));

	texture->generateMipmaps(*app->engine);

	return texture;
}

void replaceAll(std::string& shader, const std::string& from, const std::string& to) {

	size_t pos = shader.find(from);
	for (; pos != std::string::npos; pos = shader.find(from, pos)) {
		shader.replace(pos, from.length(), to);
	}
}


Package BuildMaterial(FilamentApp* app, ::MaterialInfo& info) {
	bool hasUV = false;

	MaterialBuilder::init();
	MaterialBuilder builder;
	builder
		.name("DefaultMaterial")
		.targetApi(MaterialBuilder::TargetApi::ALL)
		.multiBounceAmbientOcclusion(info.multiBounceAO)
		.specularAmbientOcclusion(info.specularAO)
		.shading(Shading::LIT)
		.specularAntiAliasing(info.specularAntiAliasing)
		.doubleSided(info.doubleSided)
		.clearCoatIorChange(info.clearCoatIorChange)
		.reflectionMode(info.screenSpaceReflection ? ReflectionMode::SCREEN_SPACE : ReflectionMode::DEFAULT)
		.transparencyMode(info.doubleSided ? TransparencyMode::TWO_PASSES_TWO_SIDES : TransparencyMode::DEFAULT)
		.targetApi(targetApiFromBackend(app->engine->getBackend()))
		.blending(info.blending);

	builder.parameter("baseColor", MaterialBuilder::UniformType::FLOAT4);

	std::string shader = R"SHADER(
        void material(inout MaterialInputs material) {
    )SHADER";

	shader += R"SHADER(
            vec2 uv0 = ${uv};
			uv0.y = 1.0 - uv0.y;
			material.baseColor = materialParams.baseColor;
        )SHADER";

	if (info.normalMap.data.data != nullptr) {
		shader += R"SHADER(
            material.normal = texture(materialParams_normalMap, uv0).xyz * 2.0 - 1.0;
            material.normal.y *= -1.0;
        )SHADER";

		builder.parameter("normalMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		hasUV = true;
	}

	shader += R"SHADER(
        prepareMaterial(material);
    )SHADER";


	if (info.baseColorMap.data.data != nullptr) {
		shader += R"SHADER(
            material.baseColor.rgb *= texture(materialParams_baseColorMap, uv0).rgb;
        )SHADER";
		builder.parameter("baseColorMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		hasUV = true;
	}

	if (info.metallicRoughnessMap.data.data != nullptr) {
		shader += R"SHADER(
            material.metallic = texture(materialParams_metallicRoughnessMap, uv0).r;
            material.roughness = texture(materialParams_metallicRoughnessMap, uv0).g;
        )SHADER";
		builder.parameter("metallicRoughnessMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		hasUV = true;
	}
	else {
		shader += R"SHADER(
            material.metallic = 0.0;
            material.roughness = 0.4;
        )SHADER";
	}

	if (info.aoMap.data.data != nullptr) {
		shader += R"SHADER(
            material.ambientOcclusion = texture(materialParams_aoMap, uv0).r;
        )SHADER";
		builder.parameter("aoMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		hasUV = true;
	}
	else {
		shader += R"SHADER(
            material.ambientOcclusion = 1.0;
        )SHADER";
	}

	if (info.clearCoat) {
		shader += R"SHADER(
            material.clearCoat = 1.0;
        )SHADER";
	}
	if (info.anisotropy) {
		shader += R"SHADER(
            material.anisotropy = 0.7;
        )SHADER";
	}
	shader += "}\n";

	if (hasUV) {
		builder.require(filament::VertexAttribute::UV0);
		replaceAll(shader, "${uv}", "getUV0()");
	}
	else
		replaceAll(shader, "${uv}", "vec2(0)");


	builder.material(shader.c_str());

	return builder.build(app->engine->getJobSystem());
}


void AddMaterial(FilamentApp* app, OBJID id, ::MaterialInfo& info)
{
	std::string hash("pbr");

	if (info.normalMap.data.data != nullptr)
		hash += "_nm";

	if (info.baseColorMap.data.data != nullptr)
		hash += "_cm";

	if (info.metallicRoughnessMap.data.data != nullptr)
		hash += "_mrm";

	if (info.aoMap.data.data != nullptr)
		hash += "_aom";

	Material* flMat;

	if (!app->materials.contains(hash)) {

		Package package;
		bool mustBuild = true;

		if (app->materialCachePath.length() > 0) {

			std::string fileName = app->materialCachePath + "/" + hash + ".mat";

			struct stat buffer;

			if (stat(fileName.c_str(), &buffer) == 0) {

				FILE* fd;
				
				if (fopen_s(&fd, fileName.c_str(), "rb") != 0)
					throw "";

				fseek(fd, 0L, SEEK_END);
				auto sz = ftell(fd);
				rewind(fd);
				auto pack = new uint8_t[sz];
				fread(pack, 1, sz, fd);
				fclose(fd);

				package = Package(pack, sz);

				mustBuild = false;
			}
		}
		if (mustBuild) {
			package = BuildMaterial(app, info);
			if (app->materialCachePath.length() > 0) {

				std::string fileName = app->materialCachePath + "/" + hash + ".mat";

				FILE* fd;
				if (fopen_s(&fd, fileName.c_str(), "wb") == 0) {

					fwrite(package.getData(), 1, package.getSize(), fd);
					fflush(fd);
					fclose(fd);
				}
	
			}
		}

		flMat = Material::Builder()
			.package(package.getData(), package.getSize())
			.build(*app->engine);

		app->materials[hash] = flMat;
	}
	else
		flMat = app->materials[hash];

	auto instance = flMat->createInstance();

	TextureSampler sampler(TextureSampler::MinFilter::LINEAR_MIPMAP_LINEAR,
		TextureSampler::MagFilter::LINEAR, TextureSampler::WrapMode::REPEAT);

	//sampler.setAnisotropy(8.0f);

	instance->setParameter("baseColor", RgbaType::LINEAR, { info.color.r, info.color.g, info.color.b,  info.color.a });

	if (info.normalMap.data.data != nullptr)
		instance->setParameter("normalMap", CreateTexture(app, info.normalMap), sampler);

	if (info.baseColorMap.data.data != nullptr)
		instance->setParameter("baseColorMap", CreateTexture(app, info.baseColorMap), sampler);

	if (info.metallicRoughnessMap.data.data != nullptr)
		instance->setParameter("metallicRoughnessMap", CreateTexture(app, info.metallicRoughnessMap), sampler);

	if (info.aoMap.data.data != nullptr)
		instance->setParameter("aoMap", CreateTexture(app, info.aoMap), sampler);

	app->materialsInst[id] = instance;

}

bool GetGraphicContext(FilamentApp* app, GraphicContextInfo& info)
{
	auto backend = app->engine->getBackend();
	if (backend == Backend::OPENGL) {
#ifdef _WINDOWS
		auto plat = dynamic_cast<PlatformWGL*>(app->engine->getPlatform());
		info.glCtx = plat->mContext;
		info.hdc = plat->mWhdc;
		return true;
#endif
	}
	return false;
}

void SetObjTransform(FilamentApp* app, OBJID id, Matrix4x4 matrix)
{
	auto mat = MatFromArray(matrix);
	auto& tcm = app->engine->getTransformManager();
	auto obj = app->entities[id];
	auto instance = tcm.getInstance(obj);
	tcm.setTransform(instance, mat);
}


#ifdef _WINDOWS

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#endif 
