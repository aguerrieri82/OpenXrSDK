layout(num_views = 2) in;

uniform vec4 uViewClip[2];

void main()
{
    vec4 clip = uViewClip[gl_ViewID_OVR];

    float x0;
    float x1;

    if (gl_ViewID_OVR == 0u)
    {
        x0 = clip.z;
        x1 = 1.0;
    }
    else
    {
        x0 = -1.0;
        x1 = clip.x;
    }

    vec2 p;
    switch (gl_VertexID)
    {
        case 0: p = vec2(x0, -1.0); break;
        case 1: p = vec2(x1, -1.0); break;
        case 2: p = vec2(x0,  1.0); break;
        case 3: p = vec2(x0,  1.0); break;
        case 4: p = vec2(x1, -1.0); break;
        default:p = vec2(x1,  1.0); break;
    }

    gl_Position = vec4(p, -1.0, 1.0);
}