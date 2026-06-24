
layout (location = 0) in vec3 a_position;
layout (location = 2) in vec2 a_texcoord_0;


#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    
    layout(num_views=NUM_VIEWS) in;

#endif

out vec2 fUv;

void main()
{
    fUv = a_texcoord_0;

    vec2 clip;
    clip.x = a_position.x >= 0.0 ? 1.0 : -1.0;
    clip.y = a_position.y >= 0.0 ? 1.0 : -1.0;

    gl_Position = vec4(clip, -1.0, 1.0);
}