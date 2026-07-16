layout(location = 0) in vec2 aPosition;

struct GpuVoxelFaceInstance
{
    ivec3 Pos;
    int Face;

    vec4 BaseColor;

    vec3 Normal;
    float Roughness;

    float Metallic;
};

layout(std430, binding = 11) readonly buffer VoxelFaceInstanceBuffer
{
    GpuVoxelFaceInstance uInstances[];
};

uniform mat4 uViewProjection;
uniform vec3 uGridOrigin;
uniform float uVoxelSize;

uniform int uInstanceStart;
uniform int uInstanceStep;

out vec4 vBaseColor;
out vec3 vNormal;
out float vRoughness;
out float vMetallic;

void main()
{
    int physicalIndex =
        uInstanceStart +
        gl_InstanceID * uInstanceStep;

    GpuVoxelFaceInstance instance = uInstances[physicalIndex];

    vec3 localPosition;

    // 0 = -X
    // 1 = +X
    // 2 = -Y
    // 3 = +Y
    // 4 = -Z
    // 5 = +Z

    switch (instance.Face)
    {
        case 0:
            localPosition = vec3(-0.5, aPosition.y, -aPosition.x);
            break;

        case 1:
            localPosition = vec3(0.5, aPosition.y, aPosition.x);
            break;

        case 2:
            localPosition = vec3(aPosition.x, -0.5, -aPosition.y);
            break;

        case 3:
            localPosition = vec3(aPosition.x, 0.5, aPosition.y);
            break;

        case 4:
            localPosition = vec3(-aPosition.x, aPosition.y, -0.5);
            break;

        default:
            localPosition = vec3(aPosition.x, aPosition.y, 0.5);
            break;
    }

    vec3 voxelCenter =
        uGridOrigin +
        (vec3(instance.Pos) + vec3(0.5)) * uVoxelSize;

    vec3 worldPosition =
        voxelCenter +
        localPosition * uVoxelSize;

    vBaseColor = instance.BaseColor;
    vNormal = instance.Normal;
    vRoughness = instance.Roughness;
    vMetallic = instance.Metallic;

    gl_Position =
        uViewProjection *
        vec4(worldPosition, 1.0);
}