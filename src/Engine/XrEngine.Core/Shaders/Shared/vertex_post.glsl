
#ifdef USE_VIEW_CLIP
    uniform vec4 uViewClip[2];
#endif


void doPost()
{

#ifdef USE_VIEW_CLIP

    vec4 clip = uViewClip[ACTIVE_EYE];

    gl_ClipDistance[1] = gl_Position.x - clip.x * gl_Position.w;
    gl_ClipDistance[2] = gl_Position.y - clip.y * gl_Position.w;
    gl_ClipDistance[3] = clip.z * gl_Position.w - gl_Position.x;
    gl_ClipDistance[4] = clip.w * gl_Position.w - gl_Position.y;

    #if defined(MULTI_VIEW) && defined(NV_MULTI_VIEW_CLIP_BUG)
        if (ACTIVE_EYE == 0u)
        {
            gl_ClipDistance[1] = 1.0;
            gl_ClipDistance[2] = 1.0;
            gl_ClipDistance[3] = 1.0;
            gl_ClipDistance[4] = 1.0;
        }
    #endif

#endif
}