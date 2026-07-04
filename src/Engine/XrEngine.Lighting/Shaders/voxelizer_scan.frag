
precision highp float;

uniform vec4 uBaseColorFactor;
uniform float uMetallicFactor;
uniform float uRoughnessFactor;

layout(location = 0) uniform sampler2D uColorMap;
layout(location = 1) uniform sampler2D uMetallicRoughnessMap;

uniform bool uHasColorMap;
uniform bool uHasMetallicRoughnessMap;

in vec2 vUv;
in vec3 vWorldNormal;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec3 outNormal;
layout(location = 2) out vec3 outMaterial;

void main()
{
    vec4 baseColor = uBaseColorFactor;

    if (uHasColorMap)
        baseColor *= texture(uColorMap, vUv);

    float roughness = uRoughnessFactor;
    float metallic = uMetallicFactor;

    if (uHasMetallicRoughnessMap)
    {
        vec4 mr = texture(uMetallicRoughnessMap, vUv);

        // glTF convention: G = roughness, B = metallic.
        roughness *= mr.g;
        metallic *= mr.b;
    }

    outColor = baseColor;
    outNormal = vWorldNormal;
    outMaterial = vec3(
        roughness,
        metallic,
        gl_FrontFacing ? 1.0 : 0.0);
}
