out int fViewIndex;

#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    
    #ifndef FRAGMENT_SHADER

        layout(num_views=NUM_VIEWS) in;

    #endif  

    layout(std140, binding=10) uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
        uniform vec3 position[NUM_VIEWS];
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

    float getFarPlane() 
    {
       return uMatrices.farPlane;
    }

#else

    uniform mat4 uViewProj;
    uniform vec3 uCameraPos;
    uniform float uFarPlane;    

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

#endif

#ifndef FRAGMENT_SHADER

void computePos(vec4 pos) 
{
    #ifdef MULTI_VIEW
        fViewIndex = gl_ViewID_OVR;
    #else
        fViewIndex = 0;
    #endif

    #ifdef USE_HEIGHT_MAP

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