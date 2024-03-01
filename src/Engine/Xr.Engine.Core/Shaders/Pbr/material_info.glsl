
struct MaterialInfo
{
    float ior;
    float perceptualRoughness;      // roughness value, as authored by the model creator (input to shader)
    vec3 f0;                        // full reflectance color (n incidence angle)

    float alphaRoughness;           // roughness mapped to a more linear change in the roughness (proposed by [2])
    vec3 c_diff;

    vec3 f90;                       // reflectance color at grazing angle
    float metallic;

    vec3 baseColor;

    float sheenRoughnessFactor;
    vec3 sheenColorFactor;

    vec3 clearcoatF0;
    vec3 clearcoatF90;
    float clearcoatFactor;
    vec3 clearcoatNormal;
    float clearcoatRoughness;

    // KHR_materials_specular 
    float specularWeight; // product of specularFactor and specularTexture.a

    float transmissionFactor;

    float thickness;
    vec3 attenuationColor;
    float attenuationDistance;

    // KHR_materials_iridescence
    float iridescenceFactor;
    float iridescenceIor;
    float iridescenceThickness;

    // KHR_materials_anisotropy
    vec3 anisotropicT;
    vec3 anisotropicB;
    float anisotropyStrength;
};


// Get normal, tangent and bitangent vectors.
NormalInfo getNormalInfo(vec3 v)
{
    vec2 UV = getNormalUV();
    vec2 uv_dx = dFdx(UV);
    vec2 uv_dy = dFdy(UV);

    if (length(uv_dx) <= 1e-2) {
      uv_dx = vec2(1.0, 0.0);
    }

    if (length(uv_dy) <= 1e-2) {
      uv_dy = vec2(0.0, 1.0);
    }

    vec3 t_ = (uv_dy.t * dFdx(v_Position) - uv_dx.t * dFdy(v_Position)) /
        (uv_dx.s * uv_dy.t - uv_dy.s * uv_dx.t);

    vec3 n, t, b, ng;

    // Compute geometrical TBN:
#ifdef HAS_NORMAL_VEC3
#ifdef HAS_TANGENT_VEC4
    // Trivial TBN computation, present as vertex attribute.
    // Normalize eigenvectors as matrix is linearly interpolated.
    t = normalize(v_TBN[0]);
    b = normalize(v_TBN[1]);
    ng = normalize(v_TBN[2]);
#else
    // Normals are either present as vertex attributes or approximated.
    ng = normalize(v_Normal);
    t = normalize(t_ - ng * dot(ng, t_));
    b = cross(ng, t);
#endif
#else
    ng = normalize(cross(dFdx(v_Position), dFdy(v_Position)));
    t = normalize(t_ - ng * dot(ng, t_));
    b = cross(ng, t);
#endif


    // For a back-facing surface, the tangential basis vectors are negated.
    if (gl_FrontFacing == false)
    {
        t *= -1.0;
        b *= -1.0;
        ng *= -1.0;
    }

    // Compute normals:
    NormalInfo info;
    info.ng = ng;
#ifdef HAS_NORMAL_MAP
    info.ntex = texture(uNormalSampler, UV).rgb * 2.0 - vec3(1.0);
    info.ntex *= vec3(uMaterial.NormalScale, uMaterial.NormalScale, 1.0);
    info.ntex = normalize(info.ntex);
    info.n = normalize(mat3(t, b, ng) * info.ntex);
#else
    info.n = ng;
#endif
    info.t = t;
    info.b = b;
    return info;
}


#ifdef MATERIAL_CLEARCOAT
vec3 getClearcoatNormal(NormalInfo normalInfo)
{
#ifdef HAS_CLEARCOAT_NORMAL_MAP
    vec3 n = texture(uClearcoatNormalSampler, getClearcoatNormalUV()).rgb * 2.0 - vec3(1.0);
    n *= vec3(uMaterial.ClearcoatNormalScale, uMaterial.ClearcoatNormalScale, 1.0);
    n = mat3(normalInfo.t, normalInfo.b, normalInfo.ng) * normalize(n);
    return n;
#else
    return normalInfo.ng;
#endif
}
#endif


vec4 getBaseColor()
{
    vec4 baseColor = vec4(1);

#if defined(MATERIAL_SPECULARGLOSSINESS)
    baseColor = uMaterial.DiffuseFactor;
#elif defined(MATERIAL_METALLICROUGHNESS)
    baseColor = uMaterial.BaseColorFactor;
#endif

#if defined(MATERIAL_SPECULARGLOSSINESS) && defined(HAS_DIFFUSE_MAP)
    baseColor *= texture(uDiffuseSampler, getDiffuseUV());
#elif defined(MATERIAL_METALLICROUGHNESS) && defined(HAS_BASE_COLOR_MAP)
    baseColor *= texture(uBaseColorSampler, getBaseColorUV());
#endif

    return baseColor * getVertexColor();
}


#ifdef MATERIAL_SPECULARGLOSSINESS
MaterialInfo getSpecularGlossinessInfo(MaterialInfo info)
{
    info.f0 = uMaterial.SpecularFactor;
    info.perceptualRoughness = uMaterial.GlossinessFactor;

#ifdef HAS_SPECULAR_GLOSSINESS_MAP
    vec4 sgSample = texture(uSpecularGlossinessSampler, getSpecularGlossinessUV());
    info.perceptualRoughness *= sgSample.a ; // glossiness to roughness
    info.f0 *= sgSample.rgb; // specular
#endif // ! HAS_SPECULAR_GLOSSINESS_MAP

    info.perceptualRoughness = 1.0 - info.perceptualRoughness; // 1 - glossiness
    info.c_diff = info.baseColor.rgb * (1.0 - max(max(info.f0.r, info.f0.g), info.f0.b));
    return info;
}
#endif


#ifdef MATERIAL_METALLICROUGHNESS
MaterialInfo getMetallicRoughnessInfo(MaterialInfo info)
{
    info.metallic = uMaterial.MetallicFactor;
    info.perceptualRoughness = uMaterial.RoughnessFactor;

#ifdef HAS_METALLIC_ROUGHNESS_MAP
    // Roughness is stored in the 'g' channel, metallic is stored in the 'b' channel.
    // This layout intentionally reserves the 'r' channel for (optional) occlusion map data
    vec4 mrSample = texture(uMetallicRoughnessSampler, getMetallicRoughnessUV());
    info.perceptualRoughness *= mrSample.g;
    info.metallic *= mrSample.b;
#endif

    // Achromatic f0 based on IOR.
    info.c_diff = mix(info.baseColor.rgb,  vec3(0), info.metallic);
    info.f0 = mix(info.f0, info.baseColor.rgb, info.metallic);
    return info;
}
#endif


#ifdef MATERIAL_SHEEN
MaterialInfo getSheenInfo(MaterialInfo info)
{
    info.sheenColorFactor = uMaterial.SheenColorFactor;
    info.sheenRoughnessFactor = uMaterial.SheenRoughnessFactor;

#ifdef HAS_SHEEN_COLOR_MAP
    vec4 sheenColorSample = texture(uSheenColorSampler, getSheenColorUV());
    info.sheenColorFactor *= sheenColorSample.rgb;
#endif

#ifdef HAS_SHEEN_ROUGHNESS_MAP
    vec4 sheenRoughnessSample = texture(uSheenRoughnessSampler, getSheenRoughnessUV());
    info.sheenRoughnessFactor *= sheenRoughnessSample.a;
#endif
    return info;
}
#endif


#ifdef MATERIAL_SPECULAR
MaterialInfo getSpecularInfo(MaterialInfo info)
{   
    vec4 specularTexture = vec4(1.0);
#ifdef HAS_SPECULAR_MAP
    specularTexture.a = texture(uSpecularSampler, getSpecularUV()).a;
#endif
#ifdef HAS_SPECULAR_COLOR_MAP
    specularTexture.rgb = texture(uSpecularColorSampler, getSpecularColorUV()).rgb;
#endif

    vec3 dielectricSpecularF0 = min(info.f0 * uMaterial.KHR_materials_specular_specularColorFactor * specularTexture.rgb, vec3(1.0));
    info.f0 = mix(dielectricSpecularF0, info.baseColor.rgb, info.metallic);
    info.specularWeight = uMaterial.KHR_materials_specular_specularFactor * specularTexture.a;
    info.c_diff = mix(info.baseColor.rgb, vec3(0), info.metallic);
    return info;
}
#endif


#ifdef MATERIAL_TRANSMISSION
MaterialInfo getTransmissionInfo(MaterialInfo info)
{
    info.transmissionFactor = uMaterial.TransmissionFactor;

#ifdef HAS_TRANSMISSION_MAP
    vec4 transmissionSample = texture(uTransmissionSampler, getTransmissionUV());
    info.transmissionFactor *= transmissionSample.r;
#endif
    return info;
}
#endif


#ifdef MATERIAL_VOLUME
MaterialInfo getVolumeInfo(MaterialInfo info)
{
    info.thickness = uMaterial.ThicknessFactor;
    info.attenuationColor = uMaterial.AttenuationColor;
    info.attenuationDistance = uMaterial.AttenuationDistance;

#ifdef HAS_THICKNESS_MAP
    vec4 thicknessSample = texture(uThicknessSampler, getThicknessUV());
    info.thickness *= thicknessSample.g;
#endif
    return info;
}
#endif


#ifdef MATERIAL_IRIDESCENCE
MaterialInfo getIridescenceInfo(MaterialInfo info)
{
    info.iridescenceFactor = uMaterial.IridescenceFactor;
    info.iridescenceIor = uMaterial.IridescenceIor;
    info.iridescenceThickness = uMaterial.IridescenceThicknessMaximum;

    #ifdef HAS_IRIDESCENCE_MAP
        info.iridescenceFactor *= texture(uIridescenceSampler, getIridescenceUV()).r;
    #endif

    #ifdef HAS_IRIDESCENCE_THICKNESS_MAP
        float thicknessSampled = texture(uIridescenceThicknessSampler, getIridescenceThicknessUV()).g;
        float thickness = mix(uMaterial.IridescenceThicknessMinimum, uuMaterial.IridescenceThicknessMaximum, thicknessSampled);
        info.iridescenceThickness = thickness;
    #endif

    return info;
}
#endif


#ifdef MATERIAL_CLEARCOAT
MaterialInfo getClearCoatInfo(MaterialInfo info, NormalInfo normalInfo)
{
    info.clearcoatFactor = uMaterial.ClearcoatFactor;
    info.clearcoatRoughness = uMaterial.ClearcoatRoughnessFactor;
    info.clearcoatF0 = vec3(pow((info.ior - 1.0) / (info.ior + 1.0), 2.0));
    info.clearcoatF90 = vec3(1.0);

#ifdef HAS_CLEARCOAT_MAP
    vec4 clearcoatSample = texture(uClearcoatSampler, getClearcoatUV());
    info.clearcoatFactor *= clearcoatSample.r;
#endif

#ifdef HAS_CLEARCOAT_ROUGHNESS_MAP
    vec4 clearcoatSampleRoughness = texture(uClearcoatRoughnessSampler, getClearcoatRoughnessUV());
    info.clearcoatRoughness *= clearcoatSampleRoughness.g;
#endif

    info.clearcoatNormal = getClearcoatNormal(normalInfo);
    info.clearcoatRoughness = clamp(info.clearcoatRoughness, 0.0, 1.0);
    return info;
}
#endif


#ifdef MATERIAL_IOR
MaterialInfo getIorInfo(MaterialInfo info)
{
    info.f0 = vec3(pow(( uMaterial.Ior - 1.0) /  (uMaterial.Ior + 1.0), 2.0));
    info.ior = uMaterial.Ior;
    return info;
}
#endif

#ifdef MATERIAL_ANISOTROPY
MaterialInfo getAnisotropyInfo(MaterialInfo info, NormalInfo normalInfo)
{
    vec2 direction = vec2(1.0, 0.0);
    float strengthFactor = 1.0;
#ifdef HAS_ANISOTROPY_MAP
    vec3 anisotropySample = texture(uAnisotropySampler, getAnisotropyUV()).xyz;
    direction = anisotropySample.xy * 2.0 - vec2(1.0);
    strengthFactor = anisotropySample.z;
#endif
    vec2 directionRotation = uMaterial.Anisotropy.xy; // cos(theta), sin(theta)
    mat2 rotationMatrix = mat2(directionRotation.x, directionRotation.y, -directionRotation.y, directionRotation.x);
    direction = rotationMatrix * direction.xy;

    info.anisotropicT = mat3(normalInfo.t, normalInfo.b, normalInfo.n) * normalize(vec3(direction, 0.0));
    info.anisotropicB = cross(normalInfo.ng, info.anisotropicT);
    info.anisotropyStrength = clamp(uMaterial.Anisotropy.z * strengthFactor, 0.0, 1.0);
    return info;
}
#endif


float albedoSheenScalingLUT(float NdotV, float sheenRoughnessFactor)
{
    return texture(uSheenELUT, vec2(NdotV, sheenRoughnessFactor)).r;
}
