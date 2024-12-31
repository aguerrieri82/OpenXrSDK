
#ifdef HAS_ENV_DEPTH

	layout(binding=8) uniform highp sampler2DArray envDepth;

	uniform mat4 envViewProj[2];
	uniform float envDepthBias;

	bool passEnvDepth(vec3 fragPos, uint activeEye)
	{
		    
		#ifdef MULTI_VIEW
			uint viewIndex = gl_ViewID_OVR;	
		#else
			uint viewIndex = activeEye;
		#endif

		vec4 envPos = envViewProj[viewIndex] * vec4(fragPos, 1);

		vec2 envPosClip = envPos.xy / envPos.w;
		envPosClip = envPosClip * 0.5f + 0.5f;

		float depthViewEyeZ = texture(envDepth, vec3(envPosClip, viewIndex)).r;

		float curDepth = envPos.z / envPos.w;
		curDepth = curDepth * 0.5f + 0.5f;

		return curDepth <= depthViewEyeZ + envDepthBias;
	}

#endif
