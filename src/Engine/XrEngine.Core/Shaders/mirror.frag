
#define PLANAR_REFLECTION
#define FRAGMENT_SHADER

#include "Shared/uniforms.glsl"
#include "Shared/planar_reflection.glsl"
#include "Shared/position.glsl"


in vec3 fPos;
in vec3 fNormal;
in vec2 fPlanarUv;

layout(location=0) out vec4 FragColor;

void main()
{	
	#if MIRROR_MODE == 0

	    vec3 N = normalize(fNormal);

		#ifdef DOUBLE_SIDED
			if (!gl_FrontFacing)
				N = -N;
		#endif

		vec3 cameraPos = getViewPos();
		vec3 Lo = normalize(cameraPos - fPos);
		vec3 Lr = reflect(-Lo, N);

		FragColor.rgb = planarReflection(vec3(1.0), fPos, Lr, 0.0, 0.0);
	#else

		#ifdef PLANAR_REFLECTION_MV
			vec4 reflectionColor = texture(reflectionTexture, vec3(fPlanarUv.xy, gl_ViewID_OVR));
		#else
			vec4 reflectionColor = texture(reflectionTexture, fPlanarUv.xy);
		#endif

		FragColor.rgb = reflectionColor.rgb;
		FragColor.a = 1.0f;
	#endif

	FragColor.a = 1.0f;
}