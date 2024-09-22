
#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    #define VIEW_ID gl_ViewID_OVR

    layout(num_views=NUM_VIEWS) in;

    uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
        uniform vec3 position[NUM_VIEWS];
        float farPlane;
    } uMatrices;

    void computePos() 
    {
       gl_Position = uMatrices.viewProj[VIEW_ID] * uModel * vec4(a_position, 1.0);
       #ifdef ZLOG_F
            gl_Position.z = log(ZLOG_F*gl_Position.z + 1.0) / log(ZLOG_F*uMatrices.farPlane + 1.0) * gl_Position.w;
       #endif

       #ifdef FORCE_Z
           gl_Position.z = FORCE_Z * gl_Position.w;
       #endif
    }

#else

    uniform mat4 uViewProj;
    uniform float uFarPlane;
    uniform vec3 uViewPos;

    void computePos() 
    {
       gl_Position = uViewProj * uModel * vec4(a_position, 1.0);
       #ifdef ZLOG_F
            gl_Position.z = log2(max(ZLOG_F, 1.0 + gl_Position.w)) / log2(uFarPlane + 1.0) * gl_Position.w;
       #endif

       #ifdef FORCE_Z
           gl_Position.z = FORCE_Z * gl_Position.w;
       #endif
    }

#endif
