in vec2 fUv;
in vec2 fUv2;

flat in vec4 fConst;

layout(binding=0) uniform highp sampler2DArray uTextureArray;

layout(location = 0) out vec4 color;

void main()
{
    int img0 = int(fConst.x);
    int img1 = int(fConst.y);

    vec4 c0 = img0 >= 0
        ? texture(uTextureArray, vec3(fUv, float(img0)))
        : vec4(0.0);

    vec4 c1 = img1 >= 0
        ? texture(uTextureArray, vec3(fUv2, float(img1)))
        : c0;

    color = img1 >= 0
        ? mix(c0, c1, 0.5)
        : c0;

    color = c0;
}