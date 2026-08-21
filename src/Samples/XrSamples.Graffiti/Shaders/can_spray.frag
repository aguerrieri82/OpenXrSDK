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

float JitterPercent(float value, float percent, float rnd)
{
    return value * mix(1.0 - percent, 1.0 + percent, rnd);
}

float JitterSigned(float value, float percent, float rnd)
{
    return value * percent * (rnd * 2.0 - 1.0);
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
        Keep clear forward motion, but vary speed per line.
        Example with uDotSpeed = 0.3:
        +/- 20% -> 0.24 .. 0.36
    */
    float lineSpeed =
        JitterPercent(uDotSpeed, 0.20, rndLine);

    /*
        Global forward motion + stable phase offset per line.
    */
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

    /*
        Stable randomness per dot cell on this line.
    */
    float rndA = Hash21(vec2(vLineSeed, cell));
    float rndB = Hash21(vec2(vLineSeed + 19.7, cell));
    float rndC = Hash21(vec2(vLineSeed + 43.2, cell));
    float rndD = Hash21(vec2(vLineSeed + 71.9, cell));

    /*
        Dot length randomized proportionally.
        With uDotLength = 0.002 and +/- 45%
        => about 0.0011 .. 0.0029
    */
    float dotLength =
        max(JitterPercent(uDotLength, 0.45, rndA), 0.000001);

    /*
        Small deterministic local jitter based on the gap size.
    */
    local += JitterSigned(uGapLength, 0.35, rndB);

    /*
        Soft edges, scaled to the actual dot size.
    */
    float edgeSoftness =
        min(0.0007, dotLength * 0.45);

    float dotMask =
        smoothstep(0.0, edgeSoftness, local) *
        (1.0 - smoothstep(dotLength - edgeSoftness, dotLength, local));

    /*
        Do not kill too many dots: keep sparse but not empty.
    */
    float alive =
        step(0.08, rndA);

    dotMask *= alive;

    /*
        Ramp up/down brightness inside each dot.
        0 at start, peak in the middle, 0 at end.
    */
    float dotPos01 =
        clamp(local / dotLength, 0.0, 1.0);

    float dotEnvelope =
        pow(sin(dotPos01 * 3.14159265), 1.3);

    /*
        Random alpha/intensity variation per dot.
    */
    float dotStrength =
        mix(0.45, 1.0, rndC);

    /*
        Random color brightness per dot.
        Keeps color livelier without changing hue.
    */
    float dotBrightness =
        mix(0.75, 1.35, rndD);

    float lengthFade =
        exp(-vRayLength * uRayLengthFalloff);

    float density =
        dotMask *
        dotEnvelope *
        dotStrength *
        lengthFade *
        uDensityScale;

    if (density < 0.01)
        discard;

    density = clamp(density, 0.0, 1.0);

    vec3 color =
        uPaintColor * dotBrightness;

    FragColor = vec4(color, density);
}