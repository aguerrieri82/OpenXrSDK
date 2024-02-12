
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vUv;

uniform mat4 uModel;

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#if defined(MULTI_VIEW)

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
   gl_Position = uMatrices.projection[VIEW_ID] * (uMatrices.view[VIEW_ID] * uModel * vec4(vPos, 1.0));
}


#else

uniform mat4 uView;
uniform mat4 uProjection;

void computePos() 
{
   gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
}

#endif


void main()
{
    computePos();

    fPos = vec3(uModel * vec4(vPos, 1.0));

    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
    fUv = vUv;
}