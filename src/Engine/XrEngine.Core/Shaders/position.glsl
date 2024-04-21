
#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    #define VIEW_ID gl_ViewID_OVR

    layout(num_views=NUM_VIEWS) in;

    uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
        float farPlane;
    } uMatrices;

    void computePos() 
    {
       gl_Position = uMatrices.viewProj[VIEW_ID] * uModel * vec4(a_position, 1.0);
       #ifdef ZLOG_F
            gl_Position.z = log(ZLOG_F*gl_Position.z + 1.0) / log(ZLOG_F*uMatrices.farPlane + 1.0) * gl_Position.w;
       #endif
    }

#else

    uniform mat4 uView;
    uniform mat4 uProjection;
    uniform float uFarPlane;

    void computePos() 
    {
       gl_Position = uProjection * uView * uModel * vec4(a_position, 1.0);
       #ifdef ZLOG_F
            gl_Position.z = log(ZLOG_F*gl_Position.z + 1.0) / log(ZLOG_F*uFarPlane + 1.0) * gl_Position.w;
       #endif
    }

#endif
