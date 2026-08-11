layout(num_views = 2) in;

uniform vec4 uViewClip[2];

void main()
{
    if (gl_ViewID_OVR == 1u)
    {
        // View 1 uses normal ClipDistance in the scene pass.
        // Degenerate this prefill primitive.
        gl_Position = vec4(-1.0, -1.0, 0.0, 1.0);
        return;
    }

    // View 0 forbidden rectangle:
    // [NDC -1 .. clip.x] x [clip.y .. clip.w]
    vec4 clip = uViewClip[0];

    vec2 p;
    switch (gl_VertexID)
    {
        case 0: p = vec2(-1.0,   clip.y); break;
        case 1: p = vec2(clip.x, clip.y); break;
        case 2: p = vec2(-1.0,   clip.w); break;
        case 3: p = vec2(-1.0,   clip.w); break;
        case 4: p = vec2(clip.x, clip.y); break;
        default:p = vec2(clip.x, clip.w); break;
    }

    // Near depth with normal OpenGL depth range / LESS.
    gl_Position = vec4(p, -1.0, 1.0);
}