

#include "[XrEngine.Core]Shared/uniforms.glsl"
#include "[XrEngine.Core]Shared/position.glsl"

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUv0;

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

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;


#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif

out float fEnvDepth;

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

    vec2 uv = aUv0;
    float depth = texture(uEnvDepth, vec3(uv, float(view))).r;

    vec3 worldPos = reconstructEnvWorld(uv, depth, view);

    fPos = worldPos;
    fUv = uv;
    fEnvDepth = depth;

    #ifdef USE_SHADOW_MAP
	    fPosLightSpace = uCamera.lightSpaceMatrix * vec4(worldPos, 1.0);
	#endif

    fNormal = vec3(0.0, 1.0, 0.0);

    computePos(vec4(worldPos, 1.0));
}