#include "pch.h"

struct XrIkContext
{
    std::vector<std::unique_ptr<IK_QSegment>> bones;
    std::vector<XrIkQuaternion> initialRotations;
    bool movable = false;

    ~XrIkContext()
    {
        for (auto &bone : bones)
            bone->SetParent(nullptr);
    }
};

namespace
{
    thread_local char lastError[512] = {};

    void Require(bool condition, const char *message)
    {
        if (!condition)
            throw std::invalid_argument(message);
    }

    template<class Action>
    int32_t Guard(Action action) noexcept
    {
        try
        {
            lastError[0] = '\0';
            action();
            return 0;
        }
        catch (const std::exception &error)
        {
            std::snprintf(lastError, sizeof(lastError), "%s", error.what());
        }
        catch (...)
        {
            std::snprintf(lastError, sizeof(lastError), "Unknown native IK failure.");
        }
        return -1;
    }

    Vector3d ToVector(const XrIkVector &value)
    {
        Require(std::isfinite(value.x) && std::isfinite(value.y) && std::isfinite(value.z),
                "Vector must be finite.");
        return Vector3d(value.x, value.y, value.z);
    }

    Eigen::Quaterniond ToQuaternion(const XrIkQuaternion &value)
    {
        Eigen::Quaterniond rotation(value.w, value.x, value.y, value.z);
        Require(rotation.coeffs().allFinite() && rotation.squaredNorm() > 1e-12,
                "Orientation must be finite and nonzero.");
        rotation.normalize();
        return rotation;
    }

    XrIkVector FromVector(const Vector3d &value)
    {
        return {float(value.x()), float(value.y()), float(value.z())};
    }

    XrIkQuaternion FromQuaternion(Eigen::Quaterniond value)
    {
        value.normalize();
        return {float(value.x()), float(value.y()), float(value.z()), float(value.w())};
    }

    void UpdateTransforms(XrIkContext &context)
    {
        context.bones[0]->UpdateTransform(Eigen::Affine3d::Identity());
    }

    void ReadPose(XrIkContext &context, XrIkBonePose *poses, int32_t count)
    {
        Require(poses && count == int32_t(context.bones.size()), "Incorrect pose buffer size.");
        UpdateTransforms(context);

        for (int32_t index = 0; index < count; index++)
        {
            const auto &bone = context.bones[index];
            Eigen::Quaterniond globalRotation(bone->GlobalTransform().linear());
            Eigen::Quaterniond localRotation = ToQuaternion(context.initialRotations[index]) *
                                               Eigen::Quaterniond(bone->BasisChange());

            Require(bone->GlobalStart().allFinite() && bone->GlobalEnd().allFinite() &&
                    globalRotation.coeffs().allFinite() && localRotation.coeffs().allFinite(),
                    "The solver produced a non-finite pose.");

            poses[index].local_rotation = FromQuaternion(localRotation);
            poses[index].start = FromVector(bone->GlobalStart());
            poses[index].tip.position = FromVector(bone->GlobalEnd());
            poses[index].tip.orientation = FromQuaternion(globalRotation);
        }
    }
}

int32_t xr_ik_abi_version(void)
{
    return 2;
}

const char *xr_ik_last_error(void)
{
    return lastError;
}

int32_t xr_ik_create(const XrIkBone *bones, int32_t count, XrIkContext **context)
{
    if (context)
        *context = nullptr;

    return Guard([&]
    {
        Require(context && bones && count > 0 && count <= 4096, "Invalid skeleton buffer.");

        auto created = std::make_unique<XrIkContext>();
        created->bones.reserve(count);
        created->initialRotations.reserve(count);
        double extent = 0;

        for (int32_t index = 0; index < count; index++)
        {
            const auto &description = bones[index];
            Require(index == 0 ? description.parent == -1 :
                    description.parent >= 0 && description.parent < index,
                    "Bones must form one tree in parent-before-child order.");
            Require(description.axes >= 0 && description.axes <= 7, "Invalid joint axes.");
            Require(description.limited_axes >= 0 &&
                    (description.limited_axes & ~description.axes) == 0, "Invalid limit axes.");
            Require(std::isfinite(description.length) && description.length >= 0,
                    "Bone length must be finite and nonnegative.");

            auto offset = ToVector(description.offset);
            auto rest = ToQuaternion(description.rest_orientation).toRotationMatrix();
            auto rotationQuaternion = ToQuaternion(description.initial_rotation);
            auto rotation = rotationQuaternion.toRotationMatrix();
            auto minimum = ToVector(description.minimum);
            auto maximum = ToVector(description.maximum);
            auto stiffness = ToVector(description.stiffness);

            std::unique_ptr<IK_QSegment> bone(
                static_cast<IK_QSegment *>(IK_CreateSegment(description.axes)));
            Require(bool(bone), "Unable to create IK segment.");
            bone->SetTransform(offset, rest, rotation, description.length);

            for (int axis = 0; axis < 3; axis++)
            {
                Require(stiffness[axis] >= 0 && stiffness[axis] < 1,
                        "Stiffness must be in [0, 1). Use a fixed axis to lock rotation.");
                if (description.limited_axes & (1 << axis))
                {
                    Require(minimum[axis] >= -M_PI - 1e-6 && maximum[axis] <= M_PI + 1e-6 &&
                            minimum[axis] <= maximum[axis], "Invalid angular limits.");
                    bone->SetLimit(axis, minimum[axis], maximum[axis]);
                }
                IK_SetStiffness(bone.get(), static_cast<IK_SegmentAxis>(axis), float(stiffness[axis]));
            }

            if (description.parent >= 0)
                bone->SetParent(created->bones[description.parent].get());

            created->movable |= description.axes != 0;
            extent += offset.norm() + description.length;
            created->initialRotations.push_back(description.initial_rotation);
            created->bones.push_back(std::move(bone));
        }

        Require(extent > 1e-8, "Skeleton must have nonzero spatial extent.");
        UpdateTransforms(*created);
        *context = created.release();
    });
}

void xr_ik_destroy(XrIkContext *context)
{
    delete context;
}

int32_t xr_ik_get_bone_count(const XrIkContext *context)
{
    if (!context)
        return -1;
    return int32_t(context->bones.size());
}

int32_t xr_ik_reset(XrIkContext *context)
{
    return Guard([&]
    {
        Require(context, "Null solver.");
        context->bones[0]->Reset();
        UpdateTransforms(*context);
    });
}

int32_t xr_ik_set_rotations(XrIkContext *context, const XrIkQuaternion *rotations, int32_t count)
{
    return Guard([&]
    {
        Require(context && rotations && count == int32_t(context->bones.size()),
                "Incorrect rotation buffer size.");
        for (int32_t index = 0; index < count; index++)
            context->bones[index]->SetBasis(ToQuaternion(rotations[index]).toRotationMatrix());
        UpdateTransforms(*context);
    });
}

int32_t xr_ik_read_pose(XrIkContext *context, XrIkBonePose *poses, int32_t count)
{
    return Guard([&]
    {
        Require(context, "Null solver.");
        ReadPose(*context, poses, count);
    });
}

int32_t xr_ik_solve(XrIkContext *context, const XrIkTarget *targets, int32_t count,
                    const XrIkPole *pole, const XrIkOptions *options,
                    XrIkBonePose *poses, int32_t pose_count, XrIkResult *result)
{
    return Guard([&]
    {
        Require(context && options && result, "Missing solver, options or result.");
        Require(count >= 0 && count <= 8192 && (count == 0 || targets), "Invalid target buffer.");
        Require((poses && pose_count == int32_t(context->bones.size())) || (!poses && pose_count == 0),
                "Incorrect pose buffer size.");
        Require(options->maximum_iterations > 0 && options->maximum_iterations <= 10000,
                "Iteration count must be in [1, 10000].");
        Require(std::isfinite(options->position_tolerance) && options->position_tolerance >= 0 &&
                std::isfinite(options->orientation_tolerance) && options->orientation_tolerance >= 0,
                "Tolerances must be finite and nonnegative.");

        std::vector<std::unique_ptr<IK_QTask>> ownedTasks;
        std::list<IK_QTask *> tasks;
        ownedTasks.reserve(count * 2);

        for (int32_t index = 0; index < count; index++)
        {
            const auto &target = targets[index];
            Require(target.bone >= 0 && target.bone < int32_t(context->bones.size()), "Invalid target bone.");
            Require(std::isfinite(target.position_weight) && target.position_weight >= 0 &&
                    std::isfinite(target.orientation_weight) && target.orientation_weight >= 0,
                    "Target weights must be finite and nonnegative.");

            const auto *bone = context->bones[target.bone].get();
            if (target.position_weight > 0)
            {
                auto task = std::make_unique<IK_QPositionTask>(true, bone, ToVector(target.pose.position));
                task->SetWeight(target.position_weight);
                tasks.push_back(task.get());
                ownedTasks.push_back(std::move(task));
            }
            if (target.orientation_weight > 0)
            {
                auto task = std::make_unique<IK_QOrientationTask>(
                    true, bone, ToQuaternion(target.pose.orientation).toRotationMatrix());
                task->SetWeight(target.orientation_weight);
                tasks.push_back(task.get());
                ownedTasks.push_back(std::move(task));
            }
        }

        *result = {};
        IK_QJacobianSolver solver;

        if (pole)
        {
            Require(pole->bone >= 0 && pole->bone < int32_t(context->bones.size()), "Invalid pole bone.");
            Require(std::isfinite(pole->angle), "Pole angle must be finite.");
            Require(pole->compute_angle == 0 || pole->compute_angle == 1, "Invalid pole compute_angle value.");

            const XrIkTarget *positionTarget = nullptr;
            for (int32_t index = 0; index < count; index++)
            {
                if (targets[index].bone == pole->bone && targets[index].position_weight > 0)
                {
                    positionTarget = &targets[index];
                    break;
                }
            }
            Require(positionTarget, "Pole bone requires a position target for the same bone.");

            auto goal = ToVector(positionTarget->pose.position);
            auto poleGoal = ToVector(pole->position);
            solver.SetPoleVectorConstraint(context->bones[pole->bone].get(), goal, poleGoal,
                                           pole->angle, pole->compute_angle != 0);
        }

        if (!tasks.empty() && context->movable)
        {
            Require(solver.Setup(context->bones[0].get(), tasks), "Solver setup failed.");
            result->solver_converged = solver.Solve(
                context->bones[0].get(), tasks, options->position_tolerance, options->maximum_iterations);
            if (pole)
                result->pole_angle = solver.GetPoleAngle();
        }

        UpdateTransforms(*context);

        for (int32_t index = 0; index < count; index++)
        {
            const auto &target = targets[index];
            const auto &bone = context->bones[target.bone];
            if (target.position_weight > 0)
            {
                float error = float((ToVector(target.pose.position) - bone->GlobalEnd()).norm());
                result->position_error = std::max(result->position_error, error);
            }
            if (target.orientation_weight > 0)
            {
                Eigen::Quaterniond current(bone->GlobalTransform().linear());
                double cosine = std::abs(ToQuaternion(target.pose.orientation).dot(current.normalized()));
                float error = float(2 * std::acos(std::clamp(cosine, 0.0, 1.0)));
                result->orientation_error = std::max(result->orientation_error, error);
            }
        }

        result->targets_reached = result->position_error <= options->position_tolerance &&
                                  result->orientation_error <= options->orientation_tolerance;

        if (poses)
            ReadPose(*context, poses, pose_count);
    });
}
