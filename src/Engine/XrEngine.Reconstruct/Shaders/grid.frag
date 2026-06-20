in GS_OUT
{
    vec3 worldPos;
    vec2 uv;

    flat int reason;
    flat float maxEdge;
    flat float maxLen;
    flat float distanceToCapture;
    flat float frontness;
} fs_in;

layout(binding=0) uniform sampler2D uTexture;
layout(binding=1) uniform sampler2D uDepth;

uniform bool uShowRejected;
uniform float uAlpha;
uniform float uDepthBias;

out vec4 outColor;

bool IsKilledByAccumDepth()
{
    if (uDepthBias <= 0.0)
        return false;

    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 size = textureSize(uDepth, 0);

    if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y)
        return false;

    float oldDepth = texelFetch(uDepth, p, 0).r;

    // Standard depth: 1.0 means empty / far clear.
    if (oldDepth >= 0.999999)
        return false;

    float myDepth = gl_FragCoord.z;

    return abs(myDepth - oldDepth) <= uDepthBias;
}

void main()
{
    if (fs_in.reason == 0)
    {
        outColor = texture(uTexture, fs_in.uv);
        outColor.a = uAlpha;
        return;
    }

    if (!uShowRejected)
        discard;

    if (fs_in.reason == 1)
        outColor = vec4(1.0, 0.0, 0.0, 1.0);       // invalid UV
    else if (fs_in.reason == 2)
        outColor = vec4(0.0, 1.0, 0.0, 1.0);       // edge too long
    else if (fs_in.reason == 3)
        outColor = vec4(0.0, 0.0, 1.0, 1.0);       // lateral face
    else if (fs_in.reason == 4)
        outColor = vec4(1.0, 1.0, 0.0, 1.0);       // too far
    else
        outColor = vec4(1.0, 0.0, 1.0, 1.0);
}