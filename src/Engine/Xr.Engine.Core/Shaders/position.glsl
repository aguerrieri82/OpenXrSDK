
#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    #define VIEW_ID gl_ViewID_OVR

    layout(num_views=NUM_VIEWS) in;

    uniform SceneMatrices
    {
        uniform mat4 view[NUM_VIEWS];
        uniform mat4 projection[NUM_VIEWS];
    } uMatrices;

    void computePos() 
    {
       gl_Position = uMatrices.projection[VIEW_ID] * (uMatrices.view[VIEW_ID] * uModel * vec4(a_position, 1.0));
    }

#else

    uniform mat4 uView;
    uniform mat4 uProjection;

    void computePos() 
    {
       gl_Position = uProjection * uView * uModel * vec4(a_position, 1.0);
    }

#endif
