#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

layout(location=0) in vec3 aLocalPos;
layout(location=2) in vec2 aUV;

#ifdef USE_VERTEX_COLOR
    layout(location=4) in vec4 aColor;
    out vec4 fColor;
#endif

out vec2 fUv;

uniform vec3 uPoints[POINT_COUNT];
uniform mat4 uWordMatrix;

const vec3 up = vec3(0.0, 1.0, 0.0);

mat4 alignTangent(vec3 tangent, vec3 position)
{
    vec3 right = normalize(cross(up, tangent));
    vec3 correctedUp = cross(tangent, right);

    return mat4(
        right.x, right.y, right.z, 0,
        correctedUp.x, correctedUp.y, correctedUp.z, 0,
        tangent.x, tangent.y, tangent.z, 0,
        position.x, position.y, position.z, 1
    );
}

void main()
{
    //mat4 worldMatrix = uModel.worldMatrix;

    int pi = int(aLocalPos.z);

    vec3 p = uPoints[pi];
                
    vec3 tangent;

    if (pi == POINT_COUNT - 1)
        tangent = (p - uPoints[pi - 1]);
    else
        tangent = (uPoints[pi + 1] - p);

    mat4 trans = alignTangent(normalize(tangent), p);

    vec4 realPos = trans * vec4(aLocalPos.x, aLocalPos.y, 0.0, 1.0);

    vec4 pos = uWordMatrix * realPos;

	fUv = aUV;

    #ifdef USE_VERTEX_COLOR
        fColor = aColor;
    #endif

    computePos(pos);
}
