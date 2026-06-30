uniform vec4 uColor;
uniform vec3 uCenter;
uniform float uIntensity;
uniform float uRadius;
uniform float uWidth;

in vec3 fPos;

layout(location = 0) out vec4 FragColor;

const float Epsilon = 1e-5;

float pointLightAttenuation(float distance, float range)
{
    float safeRange = max(range, Epsilon);
    float d = distance / safeRange;

    float rangeFalloff = clamp(1.0 - d * d * d * d, 0.0, 1.0);

    return (rangeFalloff * rangeFalloff) / max(distance * distance, 0.01);
}

float smoothEdgeAttenuation(float distanceToCenter, float radius, float width)
{
    return 1.0 - smoothstep(radius, radius + width, distanceToCenter);
}

void main()
{
    float d = length(fPos - uCenter);

#if ATTENUATION_TYPE == 1
    float attenuation = pointLightAttenuation(d, uRadius);
#else
    float attenuation = smoothEdgeAttenuation(d, uRadius, uWidth);
#endif

    float alpha = clamp(uColor.a * attenuation * uIntensity, 0.0, 1.0);

    FragColor = vec4(uColor.rgb * uIntensity, alpha);
}