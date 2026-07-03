#version 310 es

#ifndef VOXELIZER_VIEW_COUNT
#define VOXELIZER_VIEW_COUNT 2
#endif

#if VOXELIZER_VIEW_COUNT > 1
#extension GL_OVR_multiview2 : require
layout(num_views = VOXELIZER_VIEW_COUNT) in;
#endif

precision highp float;

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord0;

uniform mat4 uWorld;
uniform mat4 uNormalMatrix;
uniform mat4 uViewProj[VOXELIZER_VIEW_COUNT];

out vec2 vUv;
out vec3 vWorldNormal;

void main()
{
    vec4 world = uWorld * vec4(aPosition, 1.0);

    vUv = aTexCoord0;
    vWorldNormal = normalize((uNormalMatrix * vec4(aNormal, 0.0)).xyz);

#if VOXELIZER_VIEW_COUNT > 1
    int viewIndex = int(gl_ViewID_OVR);
#else
    int viewIndex = 0;
#endif

    gl_Position = uViewProj[viewIndex] * world;
}
