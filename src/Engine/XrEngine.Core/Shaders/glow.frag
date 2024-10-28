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

    // Create a smooth gradient for the halo
    float glowFactor = smoothstep(uRadius, uRadius + uWidth, distanceToCenter);

    // Invert the glow factor so the halo fades outwards
    glowFactor = 1.0 - glowFactor;

    // Apply intensity to the glow color
    vec3 haloColor = uColor.rgb * glowFactor * uIntensity;

    // Output the final color with alpha
    FragColor = vec4(haloColor, glowFactor);

}