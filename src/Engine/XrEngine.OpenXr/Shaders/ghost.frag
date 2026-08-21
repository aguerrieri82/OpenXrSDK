

in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;
in vec3 fCameraPos; 

layout(std140, binding = 16) uniform HandRimParams
{
    vec4 Color;
    vec4 RimColor;
    vec4 CameraPos;

    float FillAlpha;
    float RimStart;
    float RimEnd;
    float RimPower;
};


layout(location=0) out vec4 FragColor;

void main()
{
    vec3 N = normalize(fNormal);
    vec3 V = normalize(fCameraPos.xyz - fPos);

    float rim = 1.0 - abs(dot(N, V));
    rim = pow(max(rim, 0.0), RimPower);
    rim = smoothstep(RimStart, RimEnd, rim);

    vec3 color = mix(Color.rgb, RimColor.rgb, rim);

    float alpha = max(
        FillAlpha * Color.a,
        rim * RimColor.a
    );

    if (alpha <= 0.001)
        discard;

    FragColor = vec4(color, alpha);
}