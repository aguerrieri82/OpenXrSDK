#include "[XrEngine.Core]Pbr/pbr_defaults.glsl"


layout(std140, binding = 15) uniform BrickUniforms
{
    vec2  uWallSize;
    float uNoiseScale;
    float uNoiseStrength;

    vec2  uBrickSize;
    float uOddRowOffset;
    float uSideDarkening;

    vec2  uMortarSize;
    float uBrickVariation;
    float uMortarVariation;

    vec2  uOffset;
    float uMinRoughness;
    float uNormalStrength;

    vec3  uBrickColor;
    vec3  uMortarColor;
};


float Hash12(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}


float ValueNoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);

    f = f * f * (3.0 - 2.0 * f);

    float a = Hash12(i);
    float b = Hash12(i + vec2(1.0, 0.0));
    float c = Hash12(i + vec2(0.0, 1.0));
    float d = Hash12(i + vec2(1.0, 1.0));

    return mix(
        mix(a, b, f.x),
        mix(c, d, f.x),
        f.y
    );
}



float LoadBrickMask(vec2 wallPos)
{
    vec2 pitch = uBrickSize + uMortarSize;

    float rowF = floor((wallPos.y - uOffset.y) / pitch.y);
    int row = int(rowF);

    float rowOffset = ((row & 1) != 0)
        ? uOddRowOffset
        : 0.0;

    float x = wallPos.x - uOffset.x - rowOffset;
    float y = wallPos.y - uOffset.y;

    float colF = floor(x / pitch.x);

    vec2 local;
    local.x = x - colF * pitch.x;
    local.y = y - rowF * pitch.y;

    bool isBrick =
        local.x >= 0.0 &&
        local.y >= 0.0 &&
        local.x < uBrickSize.x &&
        local.y < uBrickSize.y;

    return isBrick ? 1.0 : 0.0;
}


float LoadBrickMicroHeight(vec2 wallPos)
{
    float brickMask = LoadBrickMask(wallPos);

    float n0 = ValueNoise(wallPos * uNoiseScale);
    float n1 = ValueNoise(wallPos * uNoiseScale * 4.0);
    float n2 = ValueNoise(wallPos * uNoiseScale * 11.0);

    float n = n0 * 0.50 + n1 * 0.35 + n2 * 0.15;

    // Center around zero.
    n -= 0.5;

    // Mortar slightly rougher than brick.
    float amount = mix(1.35, 0.85, brickMask);

    return n * amount;
}


vec3 ApplyBrickNormalImperfection(vec3 normal, vec2 wallPos)
{
    if (uNormalStrength <= 0.0)
        return normal;

    // Step in wall meters. Tied to noise scale so the gradient samples
    // the procedural field at a meaningful distance.
    float h = max(0.001, 0.35 / max(uNoiseScale, 0.001));

    float hx0 = LoadBrickMicroHeight(wallPos - vec2(h, 0.0));
    float hx1 = LoadBrickMicroHeight(wallPos + vec2(h, 0.0));
    float hy0 = LoadBrickMicroHeight(wallPos - vec2(0.0, h));
    float hy1 = LoadBrickMicroHeight(wallPos + vec2(0.0, h));

    float dhdx = (hx1 - hx0) / (2.0 * h);
    float dhdy = (hy1 - hy0) / (2.0 * h);

    // Build wall-space tangent basis from screen derivatives.
    // This works even if the wall object is rotated, because fPos is in
    // the same space used by frag.normal.
    vec3 dpdx = dFdx(fPos);
    vec3 dpdy = dFdy(fPos);

    vec2 dwdx = dFdx(wallPos);
    vec2 dwdy = dFdy(wallPos);

    float det = dwdx.x * dwdy.y - dwdx.y * dwdy.x;

    if (abs(det) < 1e-12)
        return normal;

    vec3 tangentX = normalize(( dpdx * dwdy.y - dpdy * dwdx.y) / det);
    vec3 tangentY = normalize((-dpdx * dwdy.x + dpdy * dwdx.x) / det);

    vec3 perturbed = normalize(
        normal
        - tangentX * dhdx * uNormalStrength
        - tangentY * dhdy * uNormalStrength
    );

    return perturbed;
}

vec3 LoadBrickAlbedo(vec2 wallPos)
{
    vec2 pitch = uBrickSize + uMortarSize;

    float rowF = floor((wallPos.y - uOffset.y) / pitch.y);
    int row = int(rowF);

    float rowOffset = ((row & 1) != 0)
        ? uOddRowOffset
        : 0.0;

    float x = wallPos.x - uOffset.x - rowOffset;
    float y = wallPos.y - uOffset.y;

    float colF = floor(x / pitch.x);

    vec2 local;
    local.x = x - colF * pitch.x;
    local.y = y - rowF * pitch.y;

    bool isBrick =
        local.x >= 0.0 &&
        local.y >= 0.0 &&
        local.x < uBrickSize.x &&
        local.y < uBrickSize.y;

    float n0 = ValueNoise(wallPos * uNoiseScale);
    float n1 = ValueNoise(wallPos * uNoiseScale * 4.0);
    float noise = n0 * 0.65 + n1 * 0.35;

    if (!isBrick)
    {
        vec3 color = uMortarColor;
        color *= 1.0 + (noise - 0.5) * uMortarVariation;
        return max(color, vec3(0.0));
    }

    vec2 cell = vec2(colF, rowF);

    float r0 = Hash12(cell);
    float r1 = Hash12(cell + vec2(17.13, 91.77));

    vec3 color = uBrickColor;

    // Stable per-brick brightness variation.
    color *= mix(
        1.0 - uBrickVariation,
        1.0 + uBrickVariation,
        r0
    );

    // Small per-brick color disturbance.
    color += (r1 - 0.5) * uBrickVariation * 0.20;

    // Fine physical-space grain.
    color *= 1.0 + (noise - 0.5) * uNoiseStrength;

    return max(color, vec3(0.0));
}

FragmentProperties loadFragmentPropertiesBrick()
{
    FragmentProperties frag = loadFragmentProperties();

    vec2 wallUv = vec2(fUv.x, 1.0 - fUv.y);
    vec2 wallPos = (wallUv - vec2(0.5)) * uWallSize;

    vec3 wallAlbedo = LoadBrickAlbedo(wallPos);

    vec4 paint = frag.baseColor;

    vec3 albedo = mix(
        wallAlbedo,
        paint.rgb,
        clamp(paint.a, 0.0, 1.0)
    );
    
    /*
    // Procedural normal imperfection for debug.
    frag.normal = ApplyBrickNormalImperfection(
        normalize(frag.normal),
        wallPos
    );
    */

    float frontness = abs(dot(normalize(frag.normal), vec3(0.0, 0.0, 1.0)));
    float sideAmount = 1.0 - frontness;

    albedo *= mix(
        1.0,
        1.0 - uSideDarkening,
        sideAmount
    );

    frag.albedo = albedo;
    frag.baseColor.rgb = albedo;
    frag.baseColor.a = 1.0;

    //frag.roughness = max(frag.roughness, uMinRoughness);
    frag.metalness = 0.0;

    return frag;
}