#include "pch.h"

#ifdef _WINDOWS

class PlatformWGL2 : public PlatformWGL {
protected:
	void commit(SwapChain* swapChain) noexcept override {
		if (!skipSwap)
			PlatformWGL::commit(swapChain);

	}
public:
	bool skipSwap = false;
};

#endif

static inline mat4 MatFromArray(const Matrix4x4 array) {
	return mat4(array[0], array[1], array[2], array[3],
		array[4], array[5], array[6], array[7],
		array[8], array[9], array[10], array[11],
		array[12], array[13], array[14], array[15]);
}


#ifdef _WINDOWS
static void LogOut(void* caller, char const* msg) {
	OutputDebugStringA(msg);
}
#endif

FilamentApp* Initialize(const InitializeOptions& options) {

#ifdef _WINDOWS
	slog.e.setConsumer(LogOut, nullptr);
#endif

	auto app = new FilamentApp();

	app->materialCachePath = options.materialCachePath;
	app->oneViewPerTarget = options.oneViewPerTarget;
	
	Engine::Config cfg;

	if (options.enableStereo) {
		cfg.stereoscopicEyeCount = 2;
		if (options.driver == Backend::OPENGL)
			cfg.stereoscopicType = StereoscopicType::MULTIVIEW;
		else
			cfg.stereoscopicType = StereoscopicType::INSTANCED;

		app->isStereo = true;
	}
		
	auto builder = Engine::Builder();

	builder.backend((Backend)options.driver)
		.sharedContext(options.context)
		.featureLevel(FeatureLevel::FEATURE_LEVEL_3)
		.config(&cfg);

	if (options.driver == Backend::OPENGL) {

	#ifdef _WINDOWS
			builder.platform(new PlatformWGL2());
	#endif
	#ifdef __ANDROID__
			builder.platform(new PlatformEGLAndroid());
	#endif
	}
	
	app->engine = builder.build();

	app->scene = app->engine->createScene();
	app->renderer = app->engine->createRenderer();
	app->camera = app->engine->createCamera(EntityManager::get().create());

	if (options.windowHandle != nullptr)
		app->swapChain = app->engine->createSwapChain(options.windowHandle, filament::SwapChain::CONFIG_HAS_STENCIL_BUFFER | filament::SwapChain::CONFIG_SRGB_COLORSPACE);
	else
		app->swapChain = app->engine->createSwapChain(8, 8);

	MaterialBuilder::init();

	return app;
}


VIEWID AddView(FilamentApp* app, const ViewOptions& options)
{
	auto view = app->engine->createView();

	MultiSampleAntiAliasingOptions msaa;
	msaa.enabled = options.sampleCount > 1;
	msaa.sampleCount = options.sampleCount;

	view->setBlendMode(options.blendMode);
	view->setAntiAliasing(options.antiAliasing);
	view->setFrustumCullingEnabled(options.frustumCullingEnabled);
	view->setPostProcessingEnabled(options.postProcessingEnabled);
	view->setRenderQuality(options.renderQuality);
	view->setMultiSampleAntiAliasingOptions(msaa);
	view->setScreenSpaceRefractionEnabled(options.screenSpaceRefractionEnabled);
	view->setShadowingEnabled(options.shadowingEnabled);
	view->setShadowType(options.shadowType);
	view->setStencilBufferEnabled(options.stencilBufferEnabled);

	if (app->isStereo) {
		View::StereoscopicOptions stereoOpt;
		stereoOpt.enabled = true;
		view->setStereoscopicOptions(stereoOpt);
	}

	if (options.viewport.width != 0 && options.viewport.height != 0)
		view->setViewport(filament::Viewport(options.viewport.x, options.viewport.y, options.viewport.width, options.viewport.height));

	view->setScene(app->scene);
	view->setCamera(app->camera);

	if (options.renderTargetId != -1)
		view->setRenderTarget(app->renderTargets[options.renderTargetId]);

	app->views.push_back({ view, options.viewport });

	return (VIEWID)(app->views.size() - 1);
}

RTID AddRenderTarget(FilamentApp* app, const RenderTargetOptions& options)
{
	auto baseColorFactor = Texture::Builder()
		.width(options.width)
		.height(options.height)
		.levels(1)
		.sampler(options.depth > 1 ? Texture::Sampler::SAMPLER_2D_ARRAY : Texture::Sampler::SAMPLER_2D)
		.depth(options.depth)
		.usage(filament::Texture::Usage::COLOR_ATTACHMENT | filament::Texture::Usage::SAMPLEABLE)
		.format(options.format)
		.import(options.textureId)
		.build(*app->engine);

	app->engine->flushAndWait();

	auto depth = Texture::Builder()
		.width(options.width)
		.height(options.height)
		.levels(1)
		.sampler(options.depth > 1 ? Texture::Sampler::SAMPLER_2D_ARRAY : Texture::Sampler::SAMPLER_2D)
		.depth(options.depth)
		.usage(filament::Texture::Usage::DEPTH_ATTACHMENT)
		.format(filament::Texture::InternalFormat::DEPTH24)
		.build(*app->engine);

	auto rt = filament::RenderTarget::Builder()
		.texture(filament::RenderTarget::AttachmentPoint::COLOR, baseColorFactor)
		.texture(filament::RenderTarget::AttachmentPoint::DEPTH, depth)
		.build(*app->engine);

	app->renderTargets.push_back(rt);

	app->engine->flushAndWait();

	return (VIEWID)(app->renderTargets.size() - 1);
}


void ReleaseContext(FilamentApp* app, bool release)
{
#ifdef _WINDOWS

	auto plat = dynamic_cast<PlatformWGL2*>(app->engine->getPlatform());

	if (plat != nullptr) {
		plat->releaseContext = release;
		app->renderer->beginFrame(app->swapChain);
		app->renderer->endFrame();
		app->engine->flush();
		
	}
#endif
}

bool isFrameBegin = false;

void Render(FilamentApp* app, const ::RenderTarget targets[], uint32_t count, bool wait)
{
	Renderer::ClearOptions opt;
	opt.clear = true;
	opt.clearColor = { 0, 0, 0, 0 };

	app->renderer->setClearOptions(opt);

	app->renderer->beginFrame(app->swapChain);

	bool hasMainView = false;

	for (uint32_t i = 0; i < count; i++) {

		auto target = targets[i];
		auto &viewInfo = app->views[target.viewId];

		app->camera->setCustomProjection(MatFromArray(target.camera.projection), target.camera.near, target.camera.far);
		app->camera->setModelMatrix(MatFromArray(target.camera.transform));

		if (target.camera.isStereo) {

			app->camera->setEyeModelMatrix(0, MatFromArray(target.camera.eyes[0].relTransform));
			app->camera->setEyeModelMatrix(1, MatFromArray(target.camera.eyes[1].relTransform));

			mat4 projs[2];
			projs[0] = MatFromArray(target.camera.eyes[0].projection);
			projs[1] = MatFromArray(target.camera.eyes[1].projection);
			app->camera->setCustomEyeProjection(projs, 2, projs[0], target.camera.near, target.camera.far);
		}


		if (memcmp(&target.viewport, &viewInfo.viewport, sizeof(Rect)) != 0) 
		{
			viewInfo.view->setViewport(filament::Viewport(target.viewport.x, target.viewport.y, target.viewport.width, target.viewport.height));
			viewInfo.viewport = target.viewport;
		}

		if (target.renderTargetId == -1)
			hasMainView = true;

		if (!app->oneViewPerTarget)
		{
			if (target.renderTargetId == -1) 
				viewInfo.view->setRenderTarget(nullptr);
			else
				viewInfo.view->setRenderTarget(app->renderTargets[target.renderTargetId]);
		}

		app->renderer->render(viewInfo.view);
	}

#if _WINDOWS
	auto plat = dynamic_cast<PlatformWGL2*>(app->engine->getPlatform());
	if (plat != nullptr)
		plat->skipSwap = !hasMainView;
#endif

	app->renderer->endFrame(hasMainView);
	if (wait)
		app->engine->flushAndWait();
}

void AddLight(FilamentApp* app, OBJID id, const LightInfo& info)
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
		//.position({ info.position.x, info.position.y, info.position.z })
		//.falloff(info.falloffRadius)
		//.castLight(info.castLight)
		.build(*app->engine, light);

	app->scene->addEntity(light);
	app->entities[id] = light;
}


void AddGeometry(FilamentApp* app, OBJID id, const GeometryInfo& info)
{
	auto indices = info.indices;
	auto indicesCount = info.indicesCount;

	if (info.indicesCount == 0) {
		indices = new uint32_t[info.verticesCount];
		for (uint32_t i = 0; i < info.verticesCount; i++)
			indices[i] = i;
		indicesCount = info.verticesCount;
	}

	auto ib = IndexBuffer::Builder()
		.indexCount(indicesCount)
		.bufferType(IndexBuffer::IndexType::UINT)
		.build(*app->engine);

	auto ibSize = sizeof(uint32_t) * indicesCount;

	if (info.indicesCount > 0) {
		auto ibBuffer = new uint8_t[ibSize];
		memcpy(ibBuffer, info.indices, ibSize);
		ib->setBuffer(*app->engine, IndexBuffer::BufferDescriptor(ibBuffer, ibSize, 0));
	}
	else
		ib->setBuffer(*app->engine, IndexBuffer::BufferDescriptor(indices, ibSize, 0));


	auto vbBuilder = VertexBuffer::Builder()
		.vertexCount(info.verticesCount);

	auto surfBuilder = geometry::SurfaceOrientation::Builder();
	surfBuilder
		.triangles((uint3*)info.indices)
		.triangleCount(info.indicesCount / 3)
		.vertexCount(info.verticesCount);

	bool hasOrientation = false;

	Geometry result;

	for (uint32_t i = 0; i < info.layout.attributeCount; i++) {
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

	vbBuilder.bufferCount(hasOrientation ? 2 : 1);

	if (hasOrientation) {
		vbBuilder.attribute(filament::VertexAttribute::TANGENTS, 1, VertexBuffer::AttributeType::FLOAT4);
	}

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

void AddGeometryV2(FilamentApp* app, OBJID id, const GeometryInfo& info)
{
	auto indices = info.indices;
	auto indicesCount = info.indicesCount;

	if (info.indicesCount == 0) {
		indices = new uint32_t[info.verticesCount];
		for (uint32_t i = 0; i < info.verticesCount; i++)
			indices[i] = i;
		indicesCount = info.verticesCount;
	}

	auto tangentMeshBld = geometry::TangentSpaceMesh::Builder();

	tangentMeshBld.triangles((uint3*)indices)
		.triangleCount(info.indicesCount / 3)
		.algorithm((geometry::TangentSpaceMesh::Algorithm)5)
		.vertexCount(info.verticesCount);

	for (uint32_t i = 0; i < info.layout.attributeCount; i++) {
		auto& attr = info.layout.attributes[i];
		switch (attr.type)
		{
		case VertexAttributeType::Position:
			tangentMeshBld.positions((float3*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		case VertexAttributeType::Normal:
			tangentMeshBld.normals((float3*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		case VertexAttributeType::Tangent:
			tangentMeshBld.tangents((float4*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		case VertexAttributeType::UV0:
			tangentMeshBld.uvs((float2*)(info.vertices + attr.offset), info.layout.sizeByte);
			break;
		}
	};

	auto tangentMesh = tangentMeshBld
		.build();


	auto ib = IndexBuffer::Builder()
		.indexCount(tangentMesh->getTriangleCount() * 3)
		.bufferType(IndexBuffer::IndexType::UINT)
		.build(*app->engine);

	auto ibBuffer = new uint3[tangentMesh->getTriangleCount()];
	tangentMesh->getTriangles(ibBuffer);
	ib->setBuffer(*app->engine, IndexBuffer::BufferDescriptor(ibBuffer, sizeof(uint3) * tangentMesh->getTriangleCount(), 0));

	auto vb = VertexBuffer::Builder()
		.vertexCount(tangentMesh->getVertexCount())
		.bufferCount(1)
		.attribute(filament::VertexAttribute::POSITION, 0, VertexBuffer::AttributeType::FLOAT3, 0, 36)
		.attribute(filament::VertexAttribute::UV0, 0, VertexBuffer::AttributeType::FLOAT2, 12, 36)
		.attribute(filament::VertexAttribute::TANGENTS, 0, VertexBuffer::AttributeType::FLOAT2, 20, 36)
		.build(*app->engine);


	auto vbBuffer = new uint8_t[tangentMesh->getVertexCount() * 36];
	tangentMesh->getPositions((float3*)vbBuffer, 36);
	tangentMesh->getUVs((float2*)(vbBuffer + 12), 36);
	tangentMesh->getQuats((quatf*)(vbBuffer + 20), 36);

	vb->setBufferAt(*app->engine, 0, VertexBuffer::BufferDescriptor(vbBuffer, tangentMesh->getVertexCount() * 36, 0));

	geometry::TangentSpaceMesh::destroy(tangentMesh);

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

	Geometry result;

	result.ib = ib;
	result.vb = vb;
	result.box = { center, halfSize };

	app->geometries[id] = result;

}

void AddGroup(FilamentApp* app, OBJID id) {

	auto group = EntityManager::get().create();
	auto& tcm = app->engine->getTransformManager();
	//tcm.create(group);
	app->scene->addEntity(group);
	app->entities[id] = group;
}

void AddMesh(FilamentApp* app, OBJID id, const MeshInfo& info)
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

	auto& tcm = app->engine->getTransformManager();
	//tcm.create(mesh);
	app->scene->addEntity(mesh);
	app->entities[id] = mesh;
}

void SetObjParent(FilamentApp* app, OBJID id, OBJID parentId)
{
	auto& tcm = app->engine->getTransformManager();
	auto& parentObj = app->entities[parentId];
	auto& obj = app->entities[id];
	auto parentInstance = tcm.getInstance(parentObj);
	auto objInstance = tcm.getInstance(obj);
	tcm.setParent(objInstance, parentInstance);
}

static Texture* CreateTexture(FilamentApp* app, const TextureInfo& info) {

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

static void replaceAll(std::string& shader, const std::string& from, const std::string& to) {

	size_t pos = shader.find(from);
	for (; pos != std::string::npos; pos = shader.find(from, pos)) {
		shader.replace(pos, from.length(), to);
	}
}

static Package BuildMaterialDebug(FilamentApp* app, const ::MaterialInfo& info) {
	MaterialBuilder builder;
	builder
		.name("Normal")
		.flipUV(false)
		.shading(Shading::UNLIT);

	std::string shader = R"SHADER(

        void material(inout MaterialInputs material) {

            vec2 uv0 = getUV0();

            material.baseColor.rgb = texture(materialParams_normalMap, uv0).xyz * 2.0 - 1.0;

			prepareMaterial(material);
		}

        )SHADER";

	builder.require(filament::VertexAttribute::UV0);

	builder.parameter("normalMap", MaterialBuilder::SamplerType::SAMPLER_2D);

	builder.material(shader.c_str());

	return builder.build(app->engine->getJobSystem());
}


static Package BuildMaterial(FilamentApp* app, const ::MaterialInfo& info) {
	bool hasUV = false;

	MaterialBuilder builder;
	builder
		.name("DefaultMaterial")
		.flipUV(false)
		.multiBounceAmbientOcclusion(info.multiBounceAO)
		.specularAmbientOcclusion(info.specularAO)
		.shading(Shading::LIT)
		.specularAntiAliasing(info.specularAntiAliasing)
		.doubleSided(info.doubleSided)
#ifdef __ANDROID__
		.platform(MaterialBuilderBase::Platform::MOBILE)
#endif
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
			material.baseColor = materialParams.baseColor;
        )SHADER";

	if (info.normalMap.data.data != nullptr) {
		shader += R"SHADER(
            material.normal = texture(materialParams_normalMap, uv0).xyz;
            material.normal.xy *= materialParams.normalScale;
        )SHADER";

		builder.parameter("normalMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		builder.parameter("normalScale", MaterialBuilder::UniformType::FLOAT);
		builder.require(filament::VertexAttribute::TANGENTS);
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

	if (info.blending == BlendingMode::TRANSPARENT) {
		shader += R"SHADER(
            material.baseColor.rgb *= material.baseColor.a;
        )SHADER";
	}

	builder.parameter("roughnessFactor", MaterialBuilder::UniformType::FLOAT);
	builder.parameter("metallicFactor", MaterialBuilder::UniformType::FLOAT);
	builder.parameter("emissiveFactor", MaterialBuilder::UniformType::FLOAT3);
	builder.parameter("emissiveStrength", MaterialBuilder::UniformType::FLOAT);

	shader += R"SHADER(
                material.roughness = materialParams.roughnessFactor;
                material.metallic = materialParams.metallicFactor;
            )SHADER";


	if (info.metallicRoughnessMap.data.data != nullptr) {
		shader += R"SHADER(
            material.metallic *= texture(materialParams_metallicRoughnessMap, uv0).b;
            material.roughness *= texture(materialParams_metallicRoughnessMap, uv0).g;
        )SHADER";
		builder.parameter("metallicRoughnessMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		hasUV = true;
	}

	if (info.aoMap.data.data != nullptr) {

		shader += R"SHADER(
            float occlusion = texture(materialParams_aoMap, uv0).r;
            material.ambientOcclusion = 1.0 + materialParams.aoStrength * (occlusion - 1.0);
        )SHADER";
		builder.parameter("aoMap", MaterialBuilder::SamplerType::SAMPLER_2D);
		builder.parameter("aoStrength", MaterialBuilder::UniformType::FLOAT);
		hasUV = true;
	}
	else {
		shader += R"SHADER(
            material.ambientOcclusion = 1.0;
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
	builder.generateDebugInfo(true);

	return builder.build(app->engine->getJobSystem());
}


void AddMaterial(FilamentApp* app, OBJID id, const ::MaterialInfo& info) noexcept(false)
{
	std::string hash("pbr_v1_p");

	hash += std::to_string((int)app->engine->getBackend());

	if (info.normalMap.data.data != nullptr)
		hash += "_nm";

	if (info.baseColorMap.data.data != nullptr)
		hash += "_cm";

	if (info.metallicRoughnessMap.data.data != nullptr)
		hash += "_mrm";

	if (info.aoMap.data.data != nullptr)
		hash += "_aom";

	if (app->isStereo)
		hash += "_st";

	Material* flMat;

	if (app->materials.find(hash) == app->materials.end()) {

		Package package;
		bool mustBuild = true;

		if (app->materialCachePath.length() > 0) {

			std::string fileName = app->materialCachePath + "/" + hash + ".mat";

			if (std::filesystem::exists(fileName)) {

				FILE* fd;
				
				if (fopen_s(&fd, fileName.c_str(), "rb") != 0)
					throw "Backed material open failed";

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
			
			if (!package.isValid())
				throw "Material Build Error";

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


	instance->setParameter("baseColor", RgbaType::LINEAR, float4(info.baseColorFactor.r, info.baseColorFactor.g, info.baseColorFactor.b,  info.baseColorFactor.a ));
	instance->setParameter("metallicFactor", info.metallicFactor);
	instance->setParameter("roughnessFactor", info.roughnessFactor);
	instance->setParameter("emissiveStrength", info.emissiveStrength);
	instance->setParameter("emissiveFactor", float3(info.emissiveFactor.r, info.emissiveFactor.g, info.emissiveFactor.b));

	if (info.normalMap.data.data != nullptr) {
		instance->setParameter("normalMap", CreateTexture(app, info.normalMap), sampler);
		instance->setParameter("normalScale", info.normalScale);
	}
	if (info.baseColorMap.data.data != nullptr)
		instance->setParameter("baseColorMap", CreateTexture(app, info.baseColorMap), sampler);

	if (info.metallicRoughnessMap.data.data != nullptr)
		instance->setParameter("metallicRoughnessMap", CreateTexture(app, info.metallicRoughnessMap), sampler);

	if (info.aoMap.data.data != nullptr) {
		instance->setParameter("aoStrength", info.aoStrength);
		instance->setParameter("aoMap", CreateTexture(app, info.aoMap), sampler);
	}

	app->materialsInst[id] = instance;

}

bool GetGraphicContext(FilamentApp* app, GraphicContextInfo& info)
{
	auto backend = app->engine->getBackend();
	if (backend == Backend::OPENGL) {
#ifdef _WINDOWS
		auto plat = dynamic_cast<PlatformWGL*>(app->engine->getPlatform());
		if (plat != nullptr) {
			auto ctx = plat->getContext();
			info.winGL.glCtx = ctx.glContext;
			info.winGL.hdc = ctx.hDC;
			return true;
		}
#endif
	}
	else if (backend == Backend::VULKAN) {
		auto plat = reinterpret_cast<VulkanPlatform*>(app->engine->getPlatform());
		info.vulkan.instance = plat->getInstance();
		info.vulkan.device = plat->getDevice();
		info.vulkan.physicalDevice = plat->getPhysicalDevice();
		info.vulkan.queueFamily = plat->getGraphicsQueueFamilyIndex();
		info.vulkan.queue = plat->getGraphicsQueueIndex();
		return true;
	}
	return false;
}

void SetObjTransform(FilamentApp* app, OBJID id, const Matrix4x4 matrix)
{
	auto& tcm = app->engine->getTransformManager();
	auto& obj = app->entities[id];
	auto instance = tcm.getInstance(obj);
	tcm.setTransform(instance, MatFromArray(matrix));
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
