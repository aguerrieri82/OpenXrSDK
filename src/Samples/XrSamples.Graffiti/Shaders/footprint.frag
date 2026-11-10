in vec2 fUv;
in vec3 fPos;

layout(location=0) out vec4 FragColor;

uniform vec3 uPaintColor;
uniform vec3 uNozzleWorld;
uniform float uRadialFalloff;
uniform float uDistanceK;
uniform float uDensity;
uniform vec2 uMaskSize;
uniform float uMaskAngle;

void main()
{
    vec2 brushCoord = (fUv - 0.5) * 2.0;

    float maskSin = sin(-uMaskAngle);
    float maskCos = cos(-uMaskAngle);

    vec2 maskCoord = vec2(
        brushCoord.x * maskCos - brushCoord.y * maskSin,
        brushCoord.x * maskSin + brushCoord.y * maskCos) / min(uMaskSize, vec2(1.0));

    float radialSq = dot(maskCoord, maskCoord);

    if (radialSq > 1.0)
        discard;

    float radialDensity = pow(clamp(1.0 - radialSq, 0.0, 1.0), uRadialFalloff);

    float ratio = 1.0 + distance(fPos, uNozzleWorld) * uDistanceK;

    float density = radialDensity / (ratio * ratio) * uDensity;

    FragColor = vec4(uPaintColor, clamp(density, 0.0, 1.0));
}
