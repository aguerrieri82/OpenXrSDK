#ifdef USE_ALBEDO_MAP
	layout(binding=ALBEDO_SLOT) uniform sampler2D albedoTexture;
#endif

#ifdef USE_NORMAL_MAP
	layout(binding=NORMAL_SLOT) uniform sampler2D normalTexture;
#endif

#if defined(USE_METALROUGHNESS_MAP) || defined(USE_SPECULAR_MAP)
	layout(binding=METALLICROUGHNESS_SLOT) uniform sampler2D metalroughnessTexture;
#endif

#ifdef USE_OCCLUSION_MAP
	layout(binding=OCCLUSION_SLOT) uniform sampler2D occlusionTexture;
#endif

#ifdef USE_EMISSIVE_MAP
	layout(binding=EMISSIVE_SLOT) uniform sampler2D emissiveTexture;
#endif

#ifdef USE_TRANSMISSION_MAP
	layout(binding=TRANSMISSIONMAP_SLOT) uniform sampler2D transmissionTexture;
#endif

#ifdef USE_SHEEN_COLOR_MAP
	layout(binding=SHEENCOLOR_SLOT) uniform sampler2D sheenColorTexture;
#endif

#ifdef USE_SHEEN_ROUGHNESS_MAP
	layout(binding=SHEENROUGHNESS_SLOT) uniform sampler2D sheenRoughnessTexture;
#endif

#ifdef USE_CLEARCOAT_MAP
	layout(binding=CLEARCOAT_SLOT) uniform sampler2D clearCoatTexture;
#endif

#ifdef USE_CLEARCOAT_ROUGHNESS_MAP
	layout(binding=CLEARCOATROUGHNESS_SLOT) uniform sampler2D clearCoatRoughnessTexture;
#endif

#ifdef USE_CLEARCOAT_NORMAL_MAP
	layout(binding=CLEARCOATNORMAL_SLOT) uniform sampler2D clearCoatNormalTexture;
#endif

vec4 loadBaseColor()
{
	#ifdef USE_ALBEDO_MAP

		#ifdef HAS_COLORMAP_PROJ
			if (fProjCoord.w <= 0.0)
				discard;

			vec3 ndc = fProjCoord.xyz / fProjCoord.w;
			vec2 albUv = ndc.xy * 0.5 + 0.5;
			albUv.y = 1.0 - albUv.y;
		#else

			#if ALBEDO_UV_SET == 1
				vec2 albUv = fUv2;
			#else
				vec2 albUv = fUv;
			#endif

			#ifdef ALBEDO_UV_TRANSFORM
				albUv = (uTexTransform[ALBEDO_UV_TRANSFORM] * vec3(albUv, 1.0)).xy;
			#endif

		#endif

		vec4 color = texture(albedoTexture, albUv);

		#if defined(TEXTURE_FORCE_SRGB) && !defined(TEXTURE_IS_SRGB) 
    		color.rgb = sRGBToLinear(color.rgb);
		#endif

		return color * uMaterial.color;

	#else

		#ifdef COLOR_IS_SRGB
			return vec4(sRGBToLinear(uMaterial.color.rgb), uMaterial.color.a);
		#endif

		return uMaterial.color;

	#endif
}

vec3 loadFragmentNormal()
{
	vec3 N;

	#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS) && !defined(SIMPLIFIED)

		#if NORMAL_UV_SET == 1
			vec2 normalUv = fUv2;
		#else
			vec2 normalUv = fUv;
		#endif

		#ifdef NORMAL_UV_TRANSFORM
			normalUv = (uTexTransform[NORMAL_UV_TRANSFORM] * vec3(normalUv, 1.0)).xy;
		#endif

		#ifdef NORMAL_MAP_BC3

			vec4 packedNormal = texture(normalTexture, normalUv);

			packedNormal.x = packedNormal.w * packedNormal.x;
			vec2 normalXY = packedNormal.xy * 2.0 - 1.0;
			float lenSq = dot(normalXY, normalXY);
			lenSq = min(lenSq, 1.0);
			float zComponentSq = 1.0 - lenSq;
			float normalZ = sqrt(zComponentSq);

			N.xy = normalXY;
			N.z = normalZ;

		#else

			N = 2.0 * texture(normalTexture, normalUv).rgb - 1.0;

		#endif

		mat3 TBN = fTangentBasis;

		N *= vec3(uMaterial.normalScale, uMaterial.normalScale, 1.0);

		#ifdef DOUBLE_SIDED

			if (!gl_FrontFacing)
			{
				TBN[0] = -TBN[0]; // Flip tangent.
				TBN[1] = -TBN[1]; // Flip bitangent.
				TBN[2] = -TBN[2]; // Flip normal.
			}

		#endif

		N = normalize(TBN * N);

	#else

		#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)
			N = fTangentBasis[2];
		#else
			N = normalize(fNormal);
		#endif

		#ifdef DOUBLE_SIDED
			if (!gl_FrontFacing)
				N = -N;
		#endif

	#endif

	return N;
}

void loadMetalRoughness(out float metalness, out float roughness)
{
	#ifndef SIMPLIFIED

		#ifdef USE_METALROUGHNESS_MAP

			#if METALROUGHNESS_UV_SET == 1
				vec2 mrUv = fUv2;
			#else
				vec2 mrUv = fUv;
			#endif

			#ifdef METALROUGHNESS_UV_TRANSFORM
				mrUv = (uTexTransform[METALROUGHNESS_UV_TRANSFORM] * vec3(mrUv, 1.0)).xy;
			#endif

			vec4 mr = texture(metalroughnessTexture, mrUv);
			metalness = mr.b * uMaterial.metalness;
			roughness = mr.g * uMaterial.roughness;

		#elif defined(USE_SPECULAR_MAP)

			#if SPECULAR_UV_SET == 1
				vec2 spUv = fUv2;
			#else
				vec2 spUv = fUv;
			#endif

			#ifdef SPECULAR_UV_TRANSFORM
				spUv = (uTexTransform[SPECULAR_UV_TRANSFORM] * vec3(spUv, 1.0)).xy;
			#endif

			vec4 sp = texture(metalroughnessTexture, spUv);
			roughness = (1.0 - sp.r) * uMaterial.roughness;
			metalness = uMaterial.metalness;

		#else

			metalness = uMaterial.metalness;
			roughness = uMaterial.roughness;

		#endif

	#else

		metalness = uMaterial.metalness;
		roughness = uMaterial.roughness;

	#endif
}

float loadOcclusion()
{
	#ifdef USE_OCCLUSION_MAP

		#if OCCLUSION_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef OCCLUSION_UV_TRANSFORM
			uv = (uTexTransform[OCCLUSION_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		return texture(occlusionTexture, uv).r;

	#else
		return 1.0;
	#endif
}

vec4 loadEmissive()
{
	#ifdef USE_EMISSIVE_MAP

		#if EMISSIVE_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef EMISSIVE_UV_TRANSFORM
			uv = (uTexTransform[EMISSIVE_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		return texture(emissiveTexture, uv);

	#else
		return vec4(0.0);
	#endif
}

#ifdef USE_SHEEN

vec3 loadSheenColor()
{
	vec3 color = uMaterial.sheenColor;

	#ifdef USE_SHEEN_COLOR_MAP

		#if SHEEN_COLOR_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef SHEEN_COLOR_UV_TRANSFORM
			uv = (uTexTransform[SHEEN_COLOR_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		vec3 texColor = texture(sheenColorTexture, uv).rgb;

		#if !defined(TEXTURE_IS_SRGB)
			texColor = sRGBToLinear(texColor);
		#endif

		color *= texColor;

	#endif

	return color;
}

float loadSheenRoughness()
{
	float roughness = uMaterial.sheenRoughness;

	#ifdef USE_SHEEN_ROUGHNESS_MAP

		#if SHEEN_ROUGHNESS_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef SHEEN_ROUGHNESS_UV_TRANSFORM
			uv = (uTexTransform[SHEEN_ROUGHNESS_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		roughness *= texture(sheenRoughnessTexture, uv).a;

	#endif

	return roughness;
}

#endif

#ifdef USE_CLEARCOAT

float loadClearCoat()
{
	float clearCoat = uMaterial.clearCoatFactor;

	#ifdef USE_CLEARCOAT_MAP

		#if CLEARCOAT_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef CLEARCOAT_UV_TRANSFORM
			uv = (uTexTransform[CLEARCOAT_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		clearCoat *= texture(clearCoatTexture, uv).r;

	#endif

	return clearCoat;
}

float loadClearCoatRoughness()
{
	float roughness = uMaterial.clearCoatRoughnessFactor;

	#ifdef USE_CLEARCOAT_ROUGHNESS_MAP

		#if CLEARCOAT_ROUGHNESS_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef CLEARCOAT_ROUGHNESS_UV_TRANSFORM
			uv = (uTexTransform[CLEARCOAT_ROUGHNESS_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		roughness *= texture(clearCoatRoughnessTexture, uv).g;

	#endif

	return roughness;
}

vec3 loadClearCoatNormal()
{
	vec3 N;

	#if defined(USE_CLEARCOAT_NORMAL_MAP) && defined(HAS_TANGENTS) && !defined(SIMPLIFIED)

		#if CLEARCOAT_NORMAL_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef CLEARCOAT_NORMAL_UV_TRANSFORM
			uv = (uTexTransform[CLEARCOAT_NORMAL_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		N = 2.0 * texture(clearCoatNormalTexture, uv).rgb - 1.0;

		mat3 TBN = fTangentBasis;

		N *= vec3(uMaterial.clearCoatNormalScale, uMaterial.clearCoatNormalScale, 1.0);

		#ifdef DOUBLE_SIDED

			if (!gl_FrontFacing)
			{
				TBN[0] = -TBN[0];
				TBN[1] = -TBN[1];
				TBN[2] = -TBN[2];
			}

		#endif

		N = normalize(TBN * N);

	#else

		#ifdef HAS_TANGENTS
			N = normalize(fTangentBasis[2]);
		#else
			N = normalize(fNormal);
		#endif

		#ifdef DOUBLE_SIDED
			if (!gl_FrontFacing)
				N = -N;
		#endif

	#endif

	return N;
}

#endif

#ifdef USE_TRANSMISSION

float loadTransmission()
{
	float transmission = uMaterial.transmission;

	#ifdef USE_TRANSMISSION_MAP

		#if TRANSMISSION_UV_SET == 1
			vec2 uv = fUv2;
		#else
			vec2 uv = fUv;
		#endif

		#ifdef TRANSMISSION_UV_TRANSFORM
			uv = (uTexTransform[TRANSMISSION_UV_TRANSFORM] * vec3(uv, 1.0)).xy;
		#endif

		transmission *= texture(transmissionTexture, uv).r;

	#endif

	return transmission;
}

#endif

FragmentProperties loadFragmentProperties()
{
	FragmentProperties frag;

	frag.position = fPos;
	frag.uv0 = fUv;

	#if defined(HAS_UV2) || (ALBEDO_UV_SET == 1) || (NORMAL_UV_SET == 1) || (METALROUGHNESS_UV_SET == 1) || (SPECULAR_UV_SET == 1) || (OCCLUSION_UV_SET == 1) || (EMISSIVE_UV_SET == 1) || (TRANSMISSION_UV_SET == 1) || (SHEEN_COLOR_UV_SET == 1) || (SHEEN_ROUGHNESS_UV_SET == 1) || (CLEARCOAT_UV_SET == 1) || (CLEARCOAT_ROUGHNESS_UV_SET == 1) || (CLEARCOAT_NORMAL_UV_SET == 1)
		frag.uv1 = fUv2;
	#else
		frag.uv1 = fUv;
	#endif

	frag.baseColor = loadBaseColor();

	#if ALPHA_MODE == ALPHA_MASK 
		if (frag.baseColor.a < uMaterial.alphaCutoff)
			discard;
		frag.baseColor.a = 1.0;
	#endif

	frag.albedo = frag.baseColor.rgb;

	frag.emissive = loadEmissive();

	loadMetalRoughness(frag.metalness, frag.roughness);

	frag.normal = loadFragmentNormal();
	frag.occlusion = loadOcclusion();
	frag.viewDir = normalize(fCameraPos - fPos);

	#ifdef USE_SHEEN
		frag.sheenColor = loadSheenColor();
		frag.sheenRoughness = loadSheenRoughness();
	#endif

	#ifdef USE_CLEARCOAT
		frag.clearCoat = loadClearCoat();
		frag.clearCoatRoughness = loadClearCoatRoughness();
		frag.clearCoatNormal = loadClearCoatNormal();
	#endif

	#ifdef USE_TRANSMISSION
		frag.transmission = loadTransmission();
	#endif

	return frag;
}