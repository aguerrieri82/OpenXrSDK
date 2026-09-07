
#ifdef CAMERA_UNIFORMS
    
    #define ACTIVE_EYE uActiveEye

    uniform vec3 uCameraPos;
    uniform mat4 uViewProj;
    uniform float uFarPlane;
    uniform mat4 uViewProjInv;

    vec3 getViewPos()
    {
        return uCameraPos;
    }

    mat4 getViewProj()
    {
        return uViewProj;
    }

    float getFarPlane()
    {
        return uFarPlane;
    }

    mat4 getViewProjInv()
    {
        return uViewProjInv;
    }

#else

    #ifdef MULTI_VIEW

        #define NUM_VIEWS 2

        #define ACTIVE_EYE gl_ViewID_OVR

        #ifndef FRAGMENT_SHADER
            layout(num_views=NUM_VIEWS) in;
        #endif

    #else

        #define ACTIVE_EYE uCamera.activeEye

    #endif

    vec3 getViewPos()
    {
        return uCamera.eyes[ACTIVE_EYE].position;
    }

    mat4 getViewProj()
    {
        return uCamera.eyes[ACTIVE_EYE].viewProj;
    }

    float getFarPlane()
    {
        return uCamera.farPlane;
    }

    mat4 getViewProjInv()
    {
        return uCamera.eyes[ACTIVE_EYE].viewProjInv;
    }

#endif

#ifndef FRAGMENT_SHADER

void computePos(vec4 pos) 
{

    #ifdef USE_DISPLACMENT_MAP

        gl_Position = pos;

    #else
        gl_Position = getViewProj() * pos;

        #ifdef ZLOG_F
            gl_Position.z = log2(max(ZLOG_F, 1.0 + gl_Position.w)) / log2(getFarPlane() + 1.0) * gl_Position.w;
        #endif

        #ifdef FORCE_Z
            gl_Position.z = FORCE_Z * gl_Position.w;
        #endif

    #endif
}

#endif
