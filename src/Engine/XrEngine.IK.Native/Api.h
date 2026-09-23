#pragma once

struct XrIkContext;

enum XrIkAxisFlags : std::int32_t
{
    XR_IK_AXIS_X = 1,
    XR_IK_AXIS_Y = 2,
    XR_IK_AXIS_Z = 4
};

struct XrIkVector
{
    float x, y, z;
};

struct XrIkQuaternion
{
    float x, y, z, w;
};

struct XrIkPose
{
    XrIkVector position;
    XrIkQuaternion orientation;
};

struct XrIkBone
{
    std::int32_t parent;
    std::int32_t axes;

    XrIkVector offset;
    XrIkQuaternion rest_orientation;
    XrIkQuaternion initial_rotation;

    float length;

    std::int32_t limited_axes;
    XrIkVector minimum;
    XrIkVector maximum;
    XrIkVector stiffness;
};

struct XrIkTarget
{
    std::int32_t bone;
    XrIkPose pose;
    float position_weight;
    float orientation_weight;
};

struct XrIkPole
{
    std::int32_t bone;
    XrIkVector position;
    float angle;
    std::int32_t compute_angle;
};

struct XrIkBonePose
{
    XrIkQuaternion local_rotation;
    XrIkVector start;
    XrIkPose tip;
};

struct XrIkOptions
{
    std::int32_t maximum_iterations;
    float position_tolerance;
    float orientation_tolerance;
};

struct XrIkResult
{
    float position_error;
    float orientation_error;
    float pole_angle;
    std::int32_t targets_reached;
    std::int32_t solver_converged;
};

extern "C"
{
    EXPORT std::int32_t xr_ik_abi_version();
    EXPORT const char* xr_ik_last_error();

    EXPORT std::int32_t xr_ik_create(const XrIkBone* bones, std::int32_t count, XrIkContext** context);
    EXPORT void xr_ik_destroy(XrIkContext* context);

    EXPORT std::int32_t xr_ik_get_bone_count(const XrIkContext* context);
    EXPORT std::int32_t xr_ik_reset(XrIkContext* context);

    EXPORT std::int32_t xr_ik_set_rotations(XrIkContext* context, const XrIkQuaternion* rotations, std::int32_t count);
    EXPORT std::int32_t xr_ik_read_pose(XrIkContext* context, XrIkBonePose* poses, std::int32_t count);

    EXPORT std::int32_t xr_ik_solve(
        XrIkContext* context,
        const XrIkTarget* targets,
        std::int32_t count,
        const XrIkPole* pole,
        const XrIkOptions* options,
        XrIkBonePose* poses,
        std::int32_t pose_count,
        XrIkResult* result);
}