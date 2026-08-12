
layout (location = 0) in vec3 aPosition;
layout (location = 2) in vec2 aUv0;


#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    
    layout(num_views=NUM_VIEWS) in;

#endif

out vec2 fUv;

void main()
{
    fUv = aUv0;

    vec2 clip;

    clip.x = aPosition.x >= 0.0 ? 1.0 : -1.0;
    clip.y = aPosition.y >= 0.0 ? 1.0 : -1.0;

    gl_Position = vec4(clip, -1.0, 1.0);
}