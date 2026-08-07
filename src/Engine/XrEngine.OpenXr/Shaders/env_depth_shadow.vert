
#include "[XrEngine.Core]Shared/uniforms.glsl"
#include "[XrEngine.Core]Shared/position.glsl"

layout(binding = 8) uniform sampler2DArray uEnvDepth;

#ifndef MULTI_VIEW
    uniform int uViewIndex;

    int getViewIndex()
    {
        return uViewIndex;
    }
#else
    int getViewIndex()
    {
        return int(gl_ViewID_OVR);
    }
#endif

uniform mat4 uEnvViewProjInv[2];
uniform ivec2 uGridSize;   // mesh/sample resolution, e.g. 100x100

out vec3 fWorldPos;
out vec2 fUv;
out float fEnvDepth;

ivec2 getGridCoord()
{
    int cellsX = uGridSize.x - 1;

    int id = gl_VertexID;
    int cellId = id / 6;
    int corner = id % 6;

    int cellX = cellId % cellsX;
    int cellY = cellId / cellsX;

    ivec2 o;

    if (corner == 0)
        o = ivec2(0, 0);
    else if (corner == 1)
        o = ivec2(1, 0);
    else if (corner == 2)
        o = ivec2(0, 1);
    else if (corner == 3)
        o = ivec2(1, 0);
    else if (corner == 4)
        o = ivec2(1, 1);
    else
        o = ivec2(0, 1);

    return ivec2(cellX, cellY) + o;
}

vec2 getGridUv(ivec2 p)
{
    return vec2(p) / vec2(uGridSize - ivec2(1));
}

vec3 reconstructEnvWorld(vec2 uv, float depth, int view)
{
    vec4 clip = vec4(
        uv * 2.0 - 1.0,
        depth * 2.0 - 1.0,
        1.0
    );

    vec4 world = uEnvViewProjInv[view] * clip;
    return world.xyz / world.w;
}

void main()
{
    int view = getViewIndex();

    ivec2 p = getGridCoord();
    vec2 uv = getGridUv(p);

    float depth = texture(uEnvDepth, vec3(uv, float(view))).r;
    vec3 worldPos = reconstructEnvWorld(uv, depth, view);

    fWorldPos = worldPos;
    fUv = uv;
    fEnvDepth = depth;

    gl_Position = getViewProj() * vec4(worldPos, 1.0);
}