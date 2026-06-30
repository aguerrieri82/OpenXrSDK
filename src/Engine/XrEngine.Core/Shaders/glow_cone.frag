uniform vec4 uColor;
uniform vec3 uCenter;
uniform vec3 uDirection;
uniform float uIntensity;
uniform float uRange;
uniform float uInnerAngle;
uniform float uOuterAngle;

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

void main()
{
    vec3 lightDir = normalize(uDirection);

    vec3 v = fPos - uCenter;
    float dist = length(v);

    vec3 dirToPixel = v / max(dist, Epsilon);

    float cosTheta = dot(dirToPixel, lightDir);

    float cosInner = cos(uInnerAngle);
    float cosOuter = cos(uOuterAngle);

    float coneAtt = smoothstep(cosOuter, cosInner, cosTheta);

    float distAtt = pointLightAttenuation(dist, uRange);

    float glow = coneAtt * distAtt * uIntensity;

    glow = clamp(glow, 0.0, 1.0);

    FragColor = vec4(
        uColor.rgb * glow,
        uColor.a * glow
    );
}