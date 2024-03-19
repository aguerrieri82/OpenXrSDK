uniform mat4 u_ViewProjectionMatrix;
uniform mat3 u_EnvRotation;

layout (location = 0) in vec3 a_position;
out vec3 v_TexCoords;

#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    #define VIEW_ID gl_ViewID_OVR

    layout(num_views=NUM_VIEWS) in;

    uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
    } uMatrices;

    mat4 getViewProj() 
    {
       return uMatrices.viewProj[VIEW_ID];
    }

#else
    mat4 getViewProj() 
    {
       return u_ViewProjectionMatrix;
    }
#endif



void main()
{
    v_TexCoords = u_EnvRotation * a_position;
    mat4 mat = getViewProj();
    mat[3] = vec4(0.0, 0.0, 0.0, 0.1);
    vec4 pos = mat * vec4(a_position, 1.0);
    gl_Position = pos.xyww;
}
