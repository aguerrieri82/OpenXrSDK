
layout(binding=8) uniform highp sampler2DArray envDepth;

uniform mat4 uDepthViewProjInv[2];
uniform uint uActiveEye;

in vec2 inUV;
out vec3 outVector;


void main()
{	
	#ifdef MULTI_VIEW
		uint viewIndex = gl_ViewID_OVR;	
	#else
		uint viewIndex = uActiveEye;
	#endif

	vec3 clip;

	clip.xy = inUV;
    clip.z = texture(envDepth, vec3(inUV, viewIndex)).r;

	clip = clip * 2.0 - 1.0;

	vec4 proj = uDepthViewProjInv[viewIndex] * vec4(clip, 1.0);

	proj /= proj.w;

	outVector = proj.xyz;
}