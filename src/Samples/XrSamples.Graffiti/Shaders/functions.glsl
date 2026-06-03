void BuildBasis(
    vec3 n,
    out vec3 tangent,
    out vec3 bitangent)
{
    vec3 helper =
        abs(dot(n, vec3(0.0, 1.0, 0.0))) < 0.999
            ? vec3(0.0, 1.0, 0.0)
            : vec3(1.0, 0.0, 0.0);

    tangent = normalize(cross(helper, n));
    bitangent = normalize(cross(n, tangent));
}

vec4 NlerpQuat(vec4 a, vec4 b, float t)
{
    if (dot(a, b) < 0.0)
        b = -b;

    return normalize(mix(a, b, t));
}

mat3 QuatToMat3(vec4 q)
{
    q = normalize(q);

    float x = q.x;
    float y = q.y;
    float z = q.z;
    float w = q.w;

    float xx = x * x;
    float yy = y * y;
    float zz = z * z;

    float xy = x * y;
    float xz = x * z;
    float yz = y * z;

    float wx = w * x;
    float wy = w * y;
    float wz = w * z;

    return mat3(
        1.0 - 2.0 * (yy + zz),
        2.0 * (xy + wz),
        2.0 * (xz - wy),

        2.0 * (xy - wz),
        1.0 - 2.0 * (xx + zz),
        2.0 * (yz + wx),

        2.0 * (xz + wy),
        2.0 * (yz - wx),
        1.0 - 2.0 * (xx + yy)
    );
}

mat4 BuildTransform(vec3 position, vec4 rotation, vec3 scale)
{
    mat3 r = QuatToMat3(rotation);

    r[0] *= scale.x;
    r[1] *= scale.y;
    r[2] *= scale.z;

    return mat4(
        vec4(r[0], 0.0),
        vec4(r[1], 0.0),
        vec4(r[2], 0.0),
        vec4(position, 1.0)
    );
}

mat4 GetHostLocalToWorld()
{
#ifndef USE_INSTANCE
    return uHostLocalToWorld;
#else
    float t =
        uStepCount <= 1
            ? 1.0
            : (float(gl_InstanceID) + 0.5) / float(uStepCount);

    vec3 position = mix(uPrevPosition, uCurPosition, t);
    vec4 rotation = NlerpQuat(uPrevRotation, uCurRotation, t);

    return BuildTransform(position, rotation, uHostScale);
#endif
}
