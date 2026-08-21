
in vec2 vLocal;
in vec4 vColor;

layout(location = 0) out vec4 outColor;

uniform float uFadeStart; // e.g. 0.65

void main()
{
    float r = length(vLocal);

    if (r > 1.0)
        discard;

    float alpha = 1.0 - smoothstep(uFadeStart, 1.0, r);

    outColor = vec4(vColor.rgb, vColor.a * alpha);
}