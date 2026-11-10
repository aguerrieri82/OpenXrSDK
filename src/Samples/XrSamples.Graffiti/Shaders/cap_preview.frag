in vec2 fUv;

layout(location=0) out vec4 FragColor;

uniform float uNozzleRadius;
uniform float uTanAngle;
uniform float uDistanceK;
uniform float uRadialFalloff;
uniform float uDistance;
uniform vec2 uMaskSize;
uniform float uMaskAngle;
uniform float uExtent;
uniform float uDensity;

float RadialDensity(float u)
{
    return pow(clamp(1.0 - u * u, 0.0, 1.0), uRadialFalloff);
}

void main()
{
    float footprint = uNozzleRadius + uDistance * uTanAngle;

#ifdef SIDE
    float height = max(uDistance * 0.55, footprint * 2.4);

    vec2 p = vec2(1.0 - fUv.x, 0.5 - fUv.y) * vec2(height * 2.0, height);

    float radius = (uNozzleRadius + p.x * uTanAngle) * min(uMaskSize.y, 1.0);

    if (abs(p.x - uDistance) < height * 0.01 && abs(p.y) <= radius * 1.1)
    {
        FragColor = vec4(1.0, 0.0, 0.0, 1.0);
        return;
    }

    float density = p.x <= uDistance
        ? sqrt(RadialDensity(abs(p.y) / radius) / pow(1.0 + p.x * uDistanceK, 2.0))
        : 0.0;
#else
    vec2 brushCoord = (fUv - 0.5) * uExtent / footprint;

    float maskSin = sin(-uMaskAngle);
    float maskCos = cos(-uMaskAngle);

    vec2 maskCoord = vec2(
        brushCoord.x * maskCos - brushCoord.y * maskSin,
        brushCoord.x * maskSin + brushCoord.y * maskCos) / min(uMaskSize, vec2(1.0));

    float ratio = 1.0 + uDistance * uDistanceK;

    float density = clamp(RadialDensity(length(maskCoord)) / (ratio * ratio) * uDensity, 0.0, 1.0);
#endif

    FragColor = vec4(vec3(density), 1.0);
}
