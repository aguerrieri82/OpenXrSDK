#version 310 es
precision highp float;

uniform vec4 uBaseColorFactor;
uniform float uMetallicFactor;
uniform float uRoughnessFactor;

uniform sampler2D uColorMap;
uniform sampler2D uMetallicRoughnessMap;

uniform float uHasColorMap;
uniform float uHasMetallicRoughnessMap;

in vec2 vUv;
in vec3 vWorldNormal;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outMaterial;

void main()
{
    vec4 baseColor = uBaseColorFactor;

    if (uHasColorMap > 0.5)
        baseColor *= texture(uColorMap, vUv);

    float roughness = uRoughnessFactor;
    float metallic = uMetallicFactor;

    if (uHasMetallicRoughnessMap > 0.5)
    {
        vec4 mr = texture(uMetallicRoughnessMap, vUv);

        // glTF convention: G = roughness, B = metallic.
        roughness *= mr.g;
        metallic *= mr.b;
    }

    vec3 normal = normalize(vWorldNormal) * 0.5 + 0.5;

    outColor = baseColor;
    outNormal = vec4(normal, 1.0);
    outMaterial = vec4(
        roughness,
        metallic,
        gl_FrontFacing ? 1.0 : 0.0,
        baseColor.a > 0.0 ? 1.0 : 0.0);
}
