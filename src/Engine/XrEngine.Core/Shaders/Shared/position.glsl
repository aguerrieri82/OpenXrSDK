
#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    
    #ifndef FRAGMENT_SHADER

        layout(num_views=NUM_VIEWS) in;

    #endif  

    layout(std140, binding=10) uniform SceneMatrices
    {
        mat4 viewProj[NUM_VIEWS];
        vec3 position[NUM_VIEWS];
        mat4 viewProjInv[NUM_VIEWS];
        float farPlane;
    } uMatrices;

    vec3 getViewPos() 
    {
        return uMatrices.position[gl_ViewID_OVR];   
    }

    mat4 getViewProj() 
    {
        return uMatrices.viewProj[gl_ViewID_OVR];   
    }

    mat4 getViewProjInv()
    {
        return uMatrices.viewProjInv[gl_ViewID_OVR];
    }

    float getFarPlane() 
    {
        return uMatrices.farPlane;
    }

    #define ACTIVE_EYE gl_ViewID_OVR

#else

    #ifdef CAMERA_UNIFORMS

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


       #define ACTIVE_EYE uActiveEye

    #else

        vec3 getViewPos() 
        {
            return uCamera.pos;   
        }

        mat4 getViewProj() 
        {
            return uCamera.viewProj;   
        }

        float getFarPlane() 
        {
            return uCamera.farPlane;   
        }

        mat4 getViewProjInv()
        {
            return uCamera.viewProjInv;
        }

        #define ACTIVE_EYE uCamera.activeEye

    #endif

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