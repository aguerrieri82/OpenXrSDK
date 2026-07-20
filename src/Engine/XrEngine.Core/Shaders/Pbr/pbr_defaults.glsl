
layout(binding=0) uniform sampler2D albedoTexture;
layout(binding=1) uniform sampler2D normalTexture;
layout(binding=2) uniform sampler2D metalroughnessTexture;
layout(binding=3) uniform sampler2D occlusionTexture;
layout(binding=9) uniform sampler2D emissiveTexture;


#ifndef LOAD_FRAGMENT_PROPS
	#define LOAD_FRAGMENT_PROPS LoadFragmentProperties()
#endif


vec4 LoadBaseColor()
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

		#endif

		return texture(albedoTexture, albUv) * uMaterial.color;

	#else

		return uMaterial.color;

	#endif
}

vec3 LoadFragmentNormal()
{
	vec3 N;

	#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS) && !defined(SIMPLIFIED)

		#ifdef NORMAL_MAP_BC3

			vec4 packedNormal = texture(normalTexture, fUv);

			packedNormal.x = packedNormal.w * packedNormal.x;
			vec2 normalXY = packedNormal.xy * 2.0 - 1.0;
			float lenSq = dot(normalXY, normalXY);
			lenSq = min(lenSq, 1.0);
			float zComponentSq = 1.0 - lenSq;
			float normalZ = sqrt(zComponentSq);

			N.xy = normalXY;
			N.z = normalZ;

		#else

			N = 2.0 * texture(normalTexture, fUv).rgb - 1.0;

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

void LoadMetalRoughness(out float metalness, out float roughness)
{
	#ifndef SIMPLIFIED

		#ifdef USE_METALROUGHNESS_MAP

			vec4 mr = texture(metalroughnessTexture, fUv);
			metalness = clamp(mr.b * uMaterial.metalness, 0.0, 1.0);
			roughness = clamp(mr.g * uMaterial.roughness, 0.0, 1.0);

		#elif defined(USE_SPECULAR_MAP)

			vec4 sp = texture(metalroughnessTexture, fUv);
			roughness = clamp((1.0 - sp.r) * uMaterial.roughness, 0.0, 1.0);
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

float LoadOcclusion()
{
	#ifdef USE_OCCLUSION_MAP
		return texture(occlusionTexture, fUv).r;
	#else
		return 1.0;
	#endif
}


vec4 LoadEmissive()
{
	#ifdef USE_EMISSIVE_MAP
		return texture(emissiveTexture, fUv);
	#else
		return vec4(0.0);
	#endif
}

FragmentProperties LoadFragmentProperties()
{
	FragmentProperties frag;

	frag.position = fPos;
	frag.uv0 = fUv;

	#if defined(HAS_UV2) || (ALBEDO_UV_SET == 1)
		frag.uv1 = fUv2;
	#else
		frag.uv1 = fUv;
	#endif

	frag.baseColor = LoadBaseColor();

	#if ALPHA_MODE == ALPHA_MASK 
		if (frag.baseColor.a < uMaterial.alphaCutoff)
			discard;
		frag.baseColor.a = 1.0;
	#endif

	frag.albedo = frag.baseColor.rgb;

	frag.emissive = LoadEmissive();

	LoadMetalRoughness(frag.metalness, frag.roughness);

	frag.normal = LoadFragmentNormal();
	frag.occlusion = LoadOcclusion();
	frag.viewDir = normalize(fCameraPos - fPos);

	return frag;
}
