

in vec3 fPos;

uniform vec4 uColor;
uniform float uFadeEnd;
uniform float uFadeStart;
uniform float uFadeSide; // +1.0 right, -1.0 left

layout(location=0) out vec4 FragColor;

void main()
{
    float alpha = uColor.a;

    float fadeX = fPos.x * uFadeSide;

    float denom = max(abs(uFadeEnd - uFadeStart), 0.00001);
    float t = clamp((fadeX - uFadeStart) / denom, 0.0, 1.0);

    alpha *= 1.0 - t;

    FragColor = vec4(uColor.rgb, alpha);
}