#ifdef MULTI_VIEW
    layout(binding = 0) uniform highp sampler2DArray srcImage;
#else
    layout(binding = 0) uniform sampler2D srcImage;
#endif

#ifndef OUTLINE_SIZE
    #define OUTLINE_SIZE 1
#endif

uniform vec4 uColor;

layout(location = FRAG_LOCATON) out vec4 fragColor;

#ifdef MULTI_VIEW

    float FetchMask(ivec2 p)
    {
        return texelFetch(srcImage, ivec3(p, gl_ViewID_OVR), 0).r;
    }

#else

    float FetchMask(ivec2 p)
    {
        return texelFetch(srcImage, p, 0).r;
    }

#endif

bool HasMask(ivec2 p)
{
    return FetchMask(p) > 0.5;
}

void EmitOutline()
{
    fragColor = uColor;
}

void main()
{
    ivec2 c = ivec2(gl_FragCoord.xy);

    if (HasMask(c))
        discard;

#if OUTLINE_SIZE == 1

    if (
        HasMask(c + ivec2( 1,  0)) ||
        HasMask(c + ivec2(-1,  0)) ||
        HasMask(c + ivec2( 0,  1)) ||
        HasMask(c + ivec2( 0, -1)) ||

        HasMask(c + ivec2( 1,  1)) ||
        HasMask(c + ivec2(-1,  1)) ||
        HasMask(c + ivec2( 1, -1)) ||
        HasMask(c + ivec2(-1, -1))
    )
    {
        EmitOutline();
        return;
    }

#elif OUTLINE_SIZE == 2

    // Ring 1 first: cheap early return near the object.
    if (
        HasMask(c + ivec2( 1,  0)) ||
        HasMask(c + ivec2(-1,  0)) ||
        HasMask(c + ivec2( 0,  1)) ||
        HasMask(c + ivec2( 0, -1)) ||

        HasMask(c + ivec2( 1,  1)) ||
        HasMask(c + ivec2(-1,  1)) ||
        HasMask(c + ivec2( 1, -1)) ||
        HasMask(c + ivec2(-1, -1))
    )
    {
        EmitOutline();
        return;
    }

    // Ring 2: perimeter of 5x5 square.
    if (
        HasMask(c + ivec2(-2, -2)) ||
        HasMask(c + ivec2(-1, -2)) ||
        HasMask(c + ivec2( 0, -2)) ||
        HasMask(c + ivec2( 1, -2)) ||
        HasMask(c + ivec2( 2, -2)) ||

        HasMask(c + ivec2(-2, -1)) ||
        HasMask(c + ivec2( 2, -1)) ||

        HasMask(c + ivec2(-2,  0)) ||
        HasMask(c + ivec2( 2,  0)) ||

        HasMask(c + ivec2(-2,  1)) ||
        HasMask(c + ivec2( 2,  1)) ||

        HasMask(c + ivec2(-2,  2)) ||
        HasMask(c + ivec2(-1,  2)) ||
        HasMask(c + ivec2( 0,  2)) ||
        HasMask(c + ivec2( 1,  2)) ||
        HasMask(c + ivec2( 2,  2))
    )
    {
        EmitOutline();
        return;
    }

#else

    // Generic path for OUTLINE_SIZE > 2.
    // Ring order keeps early return useful.
    for (int r = 1; r <= OUTLINE_SIZE; ++r)
    {
        for (int x = -r; x <= r; ++x)
        {
            if (
                HasMask(c + ivec2(x, -r)) ||
                HasMask(c + ivec2(x,  r))
            )
            {
                EmitOutline();
                return;
            }
        }

        for (int y = -r + 1; y <= r - 1; ++y)
        {
            if (
                HasMask(c + ivec2(-r, y)) ||
                HasMask(c + ivec2( r, y))
            )
            {
                EmitOutline();
                return;
            }
        }
    }

#endif

    discard;
}