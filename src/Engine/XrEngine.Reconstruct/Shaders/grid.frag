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

uniform sampler2D uTexture;
uniform bool uShowRejected;

out vec4 outColor;

void main()
{
    if (fs_in.reason == 0)
    {
        outColor = texture(uTexture, fs_in.uv);
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