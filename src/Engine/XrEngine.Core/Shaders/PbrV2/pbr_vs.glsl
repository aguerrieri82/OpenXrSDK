
// Physically Based Rendering
// Copyright (c) 2017-2018 Michał Siejak

// Physically Based shading model: Vertex program.

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=4) in vec4 tangent;
//layout(location=3) in vec3 bitangent;
layout(location=2) in vec2 texcoord;

layout(std140, binding=0) uniform Camera
{
	mat4 viewProj;
	vec3 cameraPosition;	
} uCamera;

uniform mat4 uModel;

layout(location=0) out Vertex
{
	vec3 position;
	vec2 texcoord;
	mat3 tangentBasis;
} vout;

vec3 computeBitangent(vec3 normal, vec4 tangent)
{
    vec3 T = tangent.xyz;
    
    float handedness = tangent.w;

    vec3 bitangent = cross(normal, T) * handedness;

    return bitangent;
}


void main()
{
	vec4 pos = uModel * vec4(position, 1.0);

	vout.position = vec3(pos);
	vout.texcoord = texcoord;

	vout.tangentBasis = mat3(uModel) * mat3(tangent, computeBitangent(normal, tangent), normal);

	gl_Position = uCamera.viewProj * pos;
}
