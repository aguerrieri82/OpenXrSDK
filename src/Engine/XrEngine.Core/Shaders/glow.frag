uniform vec4 uColor;
uniform vec3 uCenter;
uniform float uIntensity;
uniform float uRadius;
uniform float uWidth;

in vec3 fPos;

layout(location=0) out vec4 FragColor;

void main()
{    
    float distanceToCenter = length(fPos - uCenter);

    float glowFactor = smoothstep(uRadius, uRadius + uWidth, distanceToCenter);

    glowFactor = 1.0 - glowFactor;

    vec3 haloColor = uColor.rgb * glowFactor * uIntensity;

    FragColor = vec4(haloColor, glowFactor);
}