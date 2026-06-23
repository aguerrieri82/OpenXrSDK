
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUv; 


uniform mat4 uWorldMatrix;

out vec3 vWorldPos;
out vec3 vWorldNormal;

const bool FLIP_ATLAS_Y = false;

void main()
{
    vec2 atlasPos = aUv * 2.0 - 1.0;

    if (FLIP_ATLAS_Y)
        atlasPos.y = -atlasPos.y;

    gl_Position = vec4(atlasPos, 0.0, 1.0);

    vec4 worldPos = uWorldMatrix * vec4(aPosition, 1.0);

    vWorldPos = worldPos.xyz;
    vWorldNormal = normalize(mat3(uWorldMatrix) * aNormal);
}