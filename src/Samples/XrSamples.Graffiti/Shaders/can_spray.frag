#include "./uniforms.glsl"

in vec3 vWorldPos;
in float vRayLength;

flat in vec3 vLineStartWorld;
flat in vec3 vLineDirWorld;
flat in float vLineSeed;

layout(location = 0) out vec4 FragColor;

uniform float uTime;
uniform float uRayLengthFalloff;
uniform float uDotLength;     // e.g. 0.002
uniform float uGapLength;     // e.g. 0.02
uniform float uDotSpeed;      // meters/sec along the ray
uniform vec3 uPaintColor;

float Hash11(float x)
{
    return fract(sin(x * 127.1) * 43758.5453123);
}

float Hash21(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void main()
{
    float distAlongLine =
        dot(vWorldPos - vLineStartWorld, vLineDirWorld);

    float period =
        max(uDotLength + uGapLength, 0.000001);

    float rndLine =
        Hash11(vLineSeed + 17.31);

    /*
        Keep global forward motion, but avoid identical conveyor-belt lines.
        With your DotSpeed = 0.3, this gives roughly 0.24..0.36 m/s.
    */
    float lineSpeed =
        uDotSpeed * mix(0.8, 1.2, rndLine);

    float dist =
        distAlongLine
        - uTime * lineSpeed
        + rndLine * period;

    float cell =
        floor(dist / period);

    float local =
        dist - cell * period;

    if (local < 0.0)
    {
        cell -= 1.0;
        local += period;
    }

    float rndA = Hash21(vec2(vLineSeed, cell));
    float rndB = Hash21(vec2(vLineSeed + 19.7, cell));
    float rndC = Hash21(vec2(vLineSeed + 43.2, cell));

    /*
        With DotLength = 0.002, do not over-randomize length.
        Otherwise dots become too noisy / disappear.
    */
    float dotLength =
        uDotLength * mix(0.65, 1.6, rndA);

    /*
        Small deterministic position jitter inside the gap.
        Keep it limited because your gap is 10x the dot length.
    */
    local += (rndB - 0.5) * uGapLength * 0.35;

    /*
        Soft edge, but not larger than the dot itself.
    */
    float edgeSoftness =
        min(0.0007, dotLength * 0.45);

    float dotMask =
        smoothstep(0.0, edgeSoftness, local) *
        (1.0 - smoothstep(dotLength - edgeSoftness, dotLength, local));

    /*
        Do not kill too many dots with your sparse pattern.
    */
    float alive =
        step(0.08, rndA);

    dotMask *= alive;

    float dotStrength =
        mix(0.45, 1.0, rndC);

    float lengthFade =
        exp(-vRayLength * uRayLengthFalloff);

    float density =
        dotMask *
        dotStrength *
        lengthFade *
        uDensityScale;

    if (density < 0.01)
        discard;

    density = clamp(density, 0.0, 1.0);

    FragColor = vec4(uPaintColor, density);
}