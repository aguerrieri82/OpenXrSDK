struct IridescenceData
{
    float factor;
    float ior;
    float thicknessMinimum;
    float thicknessMaximum;
};

layout(std140, binding = 8) uniform IridescenceUniforms
{
    IridescenceData uIridescence;
};

#ifdef USE_IRIDESCENCE_MAP
    layout(binding=14) uniform sampler2D iridescenceTexture;
#endif

#ifdef USE_IRIDESCENCE_THICKNESS_MAP
    layout(binding=15) uniform sampler2D iridescenceThicknessTexture;
#endif

const float iridescencePi = 3.141592653589793;

const mat3 iridescenceXyzToRgb = mat3(
     3.2404542, -0.9692660,  0.0556434,
    -1.5371385,  1.8760108, -0.2040259,
    -0.4985314,  0.0415560,  1.0572252
);

float iridescenceSquare(float v)
{
    return v * v;
}

vec3 iridescenceSquare(vec3 v)
{
    return v * v;
}

vec3 fresnel0ToIor(vec3 f0)
{
    vec3 s = sqrt(f0);
    return (vec3(1.0) + s) / (vec3(1.0) - s);
}

vec3 iorToFresnel0(vec3 transmittedIor, float incidentIor)
{
    return iridescenceSquare((transmittedIor - vec3(incidentIor)) / (transmittedIor + vec3(incidentIor)));
}

float iorToFresnel0(float transmittedIor, float incidentIor)
{
    return iridescenceSquare((transmittedIor - incidentIor) / (transmittedIor + incidentIor));
}

float fresnelSchlick(float f0, float cosTheta)
{
    float x = 1.0 - cosTheta;
    float x2 = x * x;
    return f0 + (1.0 - f0) * x * x2 * x2;
}

vec3 evalIridescenceSensitivity(float opd, vec3 shift)
{
    float phase = 2.0 * iridescencePi * opd * 1e-9;

    vec3 val = vec3(5.4856e-13, 4.4201e-13, 5.2481e-13);
    vec3 pos = vec3(1.6810e6, 1.7953e6, 2.2084e6);
    vec3 var = vec3(4.3278e9, 9.3046e9, 6.6121e9);

    vec3 xyz = val * sqrt(2.0 * iridescencePi * var) * cos(pos * phase + shift) * exp(-iridescenceSquare(phase) * var);
    xyz.x += 9.7470e-14 * sqrt(2.0 * iridescencePi * 4.5282e9) * cos(2.2399e6 * phase + shift.x) * exp(-4.5282e9 * iridescenceSquare(phase));

    return iridescenceXyzToRgb * (xyz / 1.0685e-7);
}

vec3 evalIridescence(float outsideIor, float filmIor, float cosTheta1, float thickness, vec3 baseF0)
{
    filmIor = mix(outsideIor, filmIor, smoothstep(0.0, 0.03, thickness));

    float sinTheta2Sq = iridescenceSquare(outsideIor / filmIor) * (1.0 - iridescenceSquare(cosTheta1));
    float cosTheta2Sq = 1.0 - sinTheta2Sq;

    if (cosTheta2Sq < 0.0)
        return vec3(1.0);

    float cosTheta2 = sqrt(cosTheta2Sq);

    float R12 = fresnelSchlick(iorToFresnel0(filmIor, outsideIor), cosTheta1);
    float T121 = 1.0 - R12;

    float phi12 = filmIor < outsideIor ? iridescencePi : 0.0;
    float phi21 = iridescencePi - phi12;

    vec3 baseIor = fresnel0ToIor(clamp(baseF0, vec3(0.0), vec3(0.9999)));
    vec3 R23 = vec3(
        fresnelSchlick(iorToFresnel0(baseIor.x, filmIor), cosTheta2),
        fresnelSchlick(iorToFresnel0(baseIor.y, filmIor), cosTheta2),
        fresnelSchlick(iorToFresnel0(baseIor.z, filmIor), cosTheta2));

    vec3 phi23 = vec3(
        baseIor.x < filmIor ? iridescencePi : 0.0,
        baseIor.y < filmIor ? iridescencePi : 0.0,
        baseIor.z < filmIor ? iridescencePi : 0.0);

    float opd = 2.0 * filmIor * thickness * cosTheta2;
    vec3 phi = vec3(phi21) + phi23;

    vec3 R123 = min(R12 * R23, vec3(0.9999));
    vec3 r123 = sqrt(R123);
    vec3 Rs = iridescenceSquare(T121) * R23 / (vec3(1.0) - R123);

    vec3 result = R12 + Rs;
    vec3 Cm = Rs - T121;

    for (int m = 1; m <= 2; ++m)
    {
        Cm *= r123;
        result += Cm * 2.0 * evalIridescenceSensitivity(float(m) * opd, float(m) * phi);
    }

    return max(result, vec3(0.0));
}

vec3 rgbMix(vec3 base, vec3 layer, vec3 rgbAlpha)
{
    float alpha = max(rgbAlpha.r, max(rgbAlpha.g, rgbAlpha.b));
    return (1.0 - alpha) * base + rgbAlpha * layer;
}

float getIridescenceFactor(vec2 uv)
{
    float factor = uIridescence.factor;

#ifdef USE_IRIDESCENCE_MAP
    factor *= texture(iridescenceTexture, uv).r;
#endif

    return factor;
}

float getIridescenceThickness(vec2 uv)
{
#ifdef USE_IRIDESCENCE_THICKNESS_MAP
    return mix(uIridescence.thicknessMinimum, uIridescence.thicknessMaximum, texture(iridescenceThicknessTexture, uv).g);
#else
    return uIridescence.thicknessMaximum;
#endif
}
