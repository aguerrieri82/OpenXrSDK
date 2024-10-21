

#ifdef PLANAR_REFLECTION

	#ifdef PLANAR_REFLECTION_MV
		layout(binding=7) uniform mediump sampler2DArray reflectionTexture;
		uniform mat4 uReflectMatrix[2];
	#else
		layout(binding=7) uniform sampler2D reflectionTexture;
		uniform mat4 uReflectMatrix;
	#endif
	

	vec3 planarReflection(vec3 color, vec3 fragPos, vec3 Lr)
	{

		#ifdef PLANAR_REFLECTION_MV
			mat4 refMatrix = uReflectMatrix[gl_ViewID_OVR];
		#else
			mat4 refMatrix = uReflectMatrix;
		#endif

			vec3 reflectPosWorld = fragPos + Lr * 100.0; // Extend the reflection ray
		 
			vec4 reflectPosClip = refMatrix * vec4(reflectPosWorld, 1.0);
		 
			vec3 projCoords = reflectPosClip.xyz / reflectPosClip.w;
		
			projCoords = projCoords * 0.5 + 0.5;

		#ifdef PLANAR_REFLECTION_MV
			vec4 reflectionColor = texture(reflectionTexture, vec3(projCoords.xy, gl_ViewID_OVR));
		#else
			vec4 reflectionColor = texture(reflectionTexture, projCoords.xy);
		#endif

		
			float fresnelFactor = pow(1.0 - cosLo, 3.0) * 0.9 + 0.1;

			float refFactor = clamp(fresnelFactor * (1.0 - roughness), 0.0, 1.0);

			refFactor = min(reflectionColor.a, refFactor);

			return mix(color, reflectionColor.rgb, refFactor);
	}

#endif