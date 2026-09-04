#include "pch.h"

using namespace physx;

namespace
{
	static constexpr PxU32 WheelFL = 0;
	static constexpr PxU32 WheelFR = 1;
	static constexpr PxU32 WheelRL = 2;
	static constexpr PxU32 WheelRR = 3;
	static constexpr PxU32 WheelCount = 4;

	static PxVec3 ToPx(const Vec3f& value)
	{
		return PxVec3(value.X, value.Y, value.Z);
	}

	static PxQuat ToPx(const Quatf& value)
	{
		return PxQuat(value.X, value.Y, value.Z, value.W);
	}

	static PxTransform ToPx(const Pose3f& value)
	{
		return PxTransform(ToPx(value.Position), ToPx(value.Orientation));
	}

	static Vec3f FromPx(const PxVec3& value)
	{
		return { value.x, value.y, value.z };
	}

	static Quatf FromPx(const PxQuat& value)
	{
		return { value.x, value.y, value.z, value.w };
	}

	static Pose3f FromPx(const PxTransform& value)
	{
		return { FromPx(value.p), FromPx(value.q) };
	}

	static float RpmToOmega(float rpm)
	{
		return rpm * PxTwoPi / 60.0f;
	}

	static float OmegaToRpm(float omega)
	{
		return omega * 60.0f / PxTwoPi;
	}

	static float WheelMoi(const VehicleWheelDesc& wheel)
	{
		return 0.5f * wheel.Mass * wheel.Radius * wheel.Radius;
	}

	static float Clamp01(float value)
	{
		return std::max(0.0f, std::min(1.0f, value));
	}

	static float ClampSigned(float value)
	{
		return std::max(-1.0f, std::min(1.0f, value));
	}

	static bool ValidateWheel(const VehicleWheelDesc& wheel)
	{
		return wheel.Radius > 0.0f &&
			wheel.Width > 0.0f &&
			wheel.Mass > 0.0f;
	}

	static bool ValidateAxle(const VehicleAxleSimpleDesc& axle)
	{
		return ValidateWheel(axle.LeftWheel) &&
			ValidateWheel(axle.RightWheel) &&
			axle.SuspensionTravel > 0.0f &&
			axle.SuspensionStiffness > 0.0f &&
			axle.SuspensionDamping >= 0.0f &&
			axle.TireFriction > 0.0f;
	}

	static bool ValidateSimpleDesc(const VehicleSimpleDesc& desc)
	{
		return ValidateAxle(desc.FrontAxle) &&
			ValidateAxle(desc.RearAxle) &&
			desc.MaxSteeringAngle > 0.0f &&
			desc.MaxMotorTorque >= 0.0f &&
			desc.MaxBrakeTorque >= 0.0f &&
			desc.MaxHandBrakeTorque >= 0.0f &&
			desc.MaxMotorRpm > desc.IdleMotorRpm &&
			desc.IdleMotorRpm >= 0.0f;
	}

	struct VehicleRuntime
	{
		vehicle2::PxVehicleAxleDescription Axles;

		vehicle2::PxVehicleCommandState Commands;
		vehicle2::PxVehicleEngineDriveTransmissionCommandState TransmissionCommands;

		vehicle2::PxVehicleWheelParams WheelParams[WheelCount];
		vehicle2::PxVehicleSuspensionParams SuspensionParams[WheelCount];
		vehicle2::PxVehicleSuspensionForceParams SuspensionForceParams[WheelCount];
		vehicle2::PxVehicleSuspensionComplianceParams SuspensionComplianceParams[WheelCount];
		vehicle2::PxVehicleTireForceParams TireForceParams[WheelCount];

		vehicle2::PxVehicleBrakeCommandResponseParams BrakeResponseParams[2];
		vehicle2::PxVehicleSteerCommandResponseParams SteerResponseParams;
		vehicle2::PxVehicleAckermannParams AckermannParams;

		vehicle2::PxVehicleSuspensionStateCalculationParams SuspensionStateCalculationParams;
		vehicle2::PxVehicleRigidBodyParams RigidBodyParams;

		vehicle2::PxVehicleEngineParams EngineParams;
		vehicle2::PxVehicleGearboxParams GearboxParams;
		vehicle2::PxVehicleAutoboxParams AutoboxParams;
		vehicle2::PxVehicleClutchParams ClutchParams;
		vehicle2::PxVehicleClutchCommandResponseParams ClutchCommandResponseParams;
		vehicle2::PxVehicleFourWheelDriveDifferentialParams DifferentialParams;

		PxReal BrakeResponseStates[WheelCount];
		PxReal SteerResponseStates[WheelCount];
		vehicle2::PxVehicleEngineDriveThrottleCommandResponseState ThrottleResponseState;

		vehicle2::PxVehicleRigidBodyState RigidBodyState;
		vehicle2::PxVehicleWheelRigidBody1dState WheelRigidBodyStates[WheelCount];
		vehicle2::PxVehicleWheelLocalPose WheelLocalPoses[WheelCount];

		vehicle2::PxVehicleEngineState EngineState;
		vehicle2::PxVehicleGearboxState GearboxState;
		vehicle2::PxVehicleAutoboxState AutoboxState;
		vehicle2::PxVehicleClutchCommandResponseState ClutchCommandResponseState;
		vehicle2::PxVehicleClutchSlipState ClutchState;
		vehicle2::PxVehicleDifferentialState DifferentialState;
		vehicle2::PxVehicleWheelConstraintGroupState WheelConstraintGroupState;

		vehicle2::PxVehicleWheelActuationState ActuationStates[WheelCount];
		vehicle2::PxVehicleRoadGeometryState RoadGeometryStates[WheelCount];
		vehicle2::PxVehicleSuspensionState SuspensionStates[WheelCount];
		vehicle2::PxVehicleSuspensionComplianceState SuspensionComplianceStates[WheelCount];
		vehicle2::PxVehicleSuspensionForce SuspensionForces[WheelCount];
		vehicle2::PxVehicleTireGripState TireGripStates[WheelCount];
		vehicle2::PxVehicleTireDirectionState TireDirectionStates[WheelCount];
		vehicle2::PxVehicleTireSpeedState TireSpeedStates[WheelCount];
		vehicle2::PxVehicleTireSlipState TireSlipStates[WheelCount];
		vehicle2::PxVehicleTireCamberAngleState TireCamberStates[WheelCount];
		vehicle2::PxVehicleTireStickyState TireStickyStates[WheelCount];
		vehicle2::PxVehicleTireForce TireForces[WheelCount];

		vehicle2::PxVehiclePhysXRoadGeometryQueryParams RoadQueryParams;
		vehicle2::PxVehiclePhysXMaterialFrictionParams MaterialFrictionParams[WheelCount];
		vehicle2::PxVehiclePhysXSuspensionLimitConstraintParams SuspensionLimitParams[WheelCount];

		vehicle2::PxVehiclePhysXActor PhysXActor;
		vehicle2::PxVehiclePhysXSteerState PhysXSteerState;
		vehicle2::PxVehiclePhysXConstraints PhysXConstraints;

		void SetToDefault()
		{
			Axles = {};

			Commands = {};
			TransmissionCommands = {};

			for (PxU32 i = 0; i < WheelCount; i++)
			{
				WheelParams[i] = {};
				SuspensionParams[i] = {};
				SuspensionForceParams[i] = {};
				SuspensionComplianceParams[i] = {};
				TireForceParams[i] = {};

				BrakeResponseStates[i] = 0.0f;
				SteerResponseStates[i] = 0.0f;

				WheelRigidBodyStates[i].setToDefault();
				WheelLocalPoses[i].setToDefault();

				ActuationStates[i].setToDefault();
				RoadGeometryStates[i].setToDefault();
				SuspensionStates[i].setToDefault();
				SuspensionComplianceStates[i].setToDefault();
				SuspensionForces[i].setToDefault();
				TireGripStates[i].setToDefault();
				TireDirectionStates[i].setToDefault();
				TireSpeedStates[i].setToDefault();
				TireSlipStates[i].setToDefault();
				TireCamberStates[i].setToDefault();
				TireStickyStates[i].setToDefault();
				TireForces[i].setToDefault();

				MaterialFrictionParams[i] = {};
				SuspensionLimitParams[i] = {};
			}

			BrakeResponseParams[0] = {};
			BrakeResponseParams[1] = {};
			SteerResponseParams = {};
			AckermannParams = {};

			SuspensionStateCalculationParams = {};
			RigidBodyParams = {};
			RigidBodyState = {};
			RigidBodyState.setToDefault();

			EngineParams = {};
			GearboxParams = {};
			AutoboxParams = {};
			ClutchParams = {};
			ClutchCommandResponseParams = {};
			DifferentialParams.setToDefault();

			ThrottleResponseState.setToDefault();
			EngineState.setToDefault();
			GearboxState.setToDefault();
			AutoboxState.setToDefault();
			ClutchCommandResponseState.setToDefault();
			ClutchState.setToDefault();
			DifferentialState.setToDefault();
			WheelConstraintGroupState.setToDefault();

			RoadQueryParams.roadGeometryQueryType = vehicle2::PxVehiclePhysXRoadGeometryQueryType::eNONE;
			RoadQueryParams.defaultFilterData = PxQueryFilterData();
			RoadQueryParams.filterDataEntries = nullptr;
			RoadQueryParams.filterCallback = nullptr;
			PhysXActor.setToDefault();
			PhysXSteerState.setToDefault();
			PhysXConstraints.setToDefault();
		}
	};

	static void ConfigureWheel(VehicleRuntime& runtime, PxU32 id, const VehicleWheelDesc& wheel)
	{
		auto& dst = runtime.WheelParams[id];

		dst.radius = wheel.Radius;
		dst.halfWidth = wheel.Width * 0.5f;
		dst.mass = wheel.Mass;
		dst.moi = WheelMoi(wheel);
		dst.dampingRate = 0.25f;
	}

	static void ConfigureSuspension(
		VehicleRuntime& runtime,
		PxU32 id,
		const VehicleWheelDesc& wheel,
		const VehicleAxleSimpleDesc& axle,
		float sprungMass,
		float gravityMagnitude,
		const PxTransform& cMassLocalPose)
	{
		auto& suspension = runtime.SuspensionParams[id];

		// Vehicle2 reads rigidBodyState.pose as actorGlobalPose * cMassLocalPose, so all
		// suspension geometry must be expressed in that mass/COM frame. wheel.Position
		// is supplied by our API in the actor/model local frame.
		const PxTransform wheelPoseMass = cMassLocalPose.transformInv(PxTransform(ToPx(wheel.Position)));
		suspension.suspensionTravelDir = cMassLocalPose.q.rotateInv(PxVec3(0.0f, -1.0f, 0.0f));
		// PhysX 5.9 ignores an exact zero-distance raycast hit.
		suspension.suspensionTravelDist = axle.SuspensionTravel + 0.01f;

		// VehicleWheelDesc::Position is the modelled wheel centre at the desired static ride pose.
		// PxVehicleSuspensionParams::suspensionAttachment is instead the wheel pose at MAX COMPRESSION.
		const float equilibriumJounce = sprungMass * gravityMagnitude / axle.SuspensionStiffness;
		const float staticJounce = PxClamp(equilibriumJounce, 0.0f, axle.SuspensionTravel);

		suspension.suspensionAttachment = wheelPoseMass;
		suspension.suspensionAttachment.p -=
			suspension.suspensionTravelDir * (suspension.suspensionTravelDist - staticJounce);
		suspension.wheelAttachment = PxTransform(PxIdentity);

		auto& force = runtime.SuspensionForceParams[id];
		force.sprungMass = sprungMass;
		force.stiffness = axle.SuspensionStiffness;
		force.damping = axle.SuspensionDamping;
	}

	static void ConfigureTire(VehicleRuntime& runtime, PxU32 id, float sprungMass, float friction)
	{
		auto& tire = runtime.TireForceParams[id];

		const float restLoad = (sprungMass + runtime.WheelParams[id].mass) * 9.81f;

		tire.latStiffX = 0.01f;
		tire.latStiffY = 18.0f * restLoad;
		tire.longStiff = 24525.0f;
		tire.camberStiff = 0.0f;
		tire.restLoad = restLoad;

		tire.frictionVsSlip[0][0] = 0.0f;
		tire.frictionVsSlip[0][1] = 1.0f;
		tire.frictionVsSlip[1][0] = 0.1f;
		tire.frictionVsSlip[1][1] = 1.0f;
		tire.frictionVsSlip[2][0] = 1.0f;
		tire.frictionVsSlip[2][1] = 1.0f;

		tire.loadFilter[0][0] = 0.0f;
		tire.loadFilter[0][1] = 0.2308f;
		tire.loadFilter[1][0] = 3.0f;
		tire.loadFilter[1][1] = 3.0f;
	}

	static float DistanceXZ(const Vec3f& a, const Vec3f& b)
	{
		const float dx = a.X - b.X;
		const float dz = a.Z - b.Z;
		return std::sqrt(dx * dx + dz * dz);
	}
}

struct VehicleWorld
{
	PxPhysics* Physics = nullptr;
	PxScene* Scene = nullptr;
	PxMaterial* DefaultMaterial = nullptr;

	vehicle2::PxVehiclePhysXSimulationContext SimulationContext;

	VehicleWorld(PxPhysics* physics, PxScene* scene, PxMaterial* material)
		: Physics(physics), Scene(scene), DefaultMaterial(material)
	{
		SimulationContext.setToDefault();

		SimulationContext.frame.lngAxis = vehicle2::PxVehicleAxes::ePosZ;
		SimulationContext.frame.latAxis = vehicle2::PxVehicleAxes::eNegX;
		SimulationContext.frame.vrtAxis = vehicle2::PxVehicleAxes::ePosY;

		PX_ASSERT(SimulationContext.frame.isValid());

		SimulationContext.scale.scale = 1.0f;
		SimulationContext.gravity = Scene->getGravity();
		SimulationContext.physxScene = Scene;
		SimulationContext.physxActorUpdateMode = vehicle2::PxVehiclePhysXActorUpdateMode::eAPPLY_ACCELERATION;
	}
};

struct Vehicle
{
	VehicleWorld* World = nullptr;
	VehicleSimpleDesc Desc = {};
	VehicleRuntime Runtime;

	PxRigidDynamic* Actor = nullptr;

	Pose3f InitialPose = {};
	VehicleGearMode GearMode = VehicleGearMode_Automatic;

	bool Initialized = false;
};


template<typename T, size_t N>
static vehicle2::PxVehicleArrayData<T> VehicleArray(T(&data)[N])
{
	vehicle2::PxVehicleArrayData<T> result;
	result.setData(data);
	return result;
}

template<typename T, size_t N>
static vehicle2::PxVehicleArrayData<const T> VehicleConstArray(const T(&data)[N])
{
	vehicle2::PxVehicleArrayData<const T> result;
	result.setData(data);
	return result;
}

static bool ConfigurePhysXVehicle(Vehicle& vehicle)
{
	auto& runtime = vehicle.Runtime;
	auto* actor = vehicle.Actor;

	if (actor->getRigidBodyFlags().isSet(PxRigidBodyFlag::eKINEMATIC))
		return false;

	actor->setActorFlag(PxActorFlag::eDISABLE_GRAVITY, true);

	runtime.PhysXActor.setToDefault();
	runtime.PhysXActor.rigidBody = actor;
	runtime.PhysXSteerState.setToDefault();
	runtime.PhysXConstraints.setToDefault();

	runtime.RoadQueryParams.roadGeometryQueryType = vehicle2::PxVehiclePhysXRoadGeometryQueryType::eRAYCAST;
	runtime.RoadQueryParams.defaultFilterData = PxQueryFilterData(
		PxFilterData(0, 0, 0, 0), PxQueryFlag::eSTATIC);
	runtime.RoadQueryParams.filterDataEntries = nullptr;
	runtime.RoadQueryParams.filterCallback = nullptr;

	for (PxU32 i = 0; i < WheelCount; i++)
	{
		const float friction = i < 2 ? vehicle.Desc.FrontAxle.TireFriction : vehicle.Desc.RearAxle.TireFriction;

		runtime.MaterialFrictionParams[i].defaultFriction = friction;
		runtime.MaterialFrictionParams[i].materialFrictions = nullptr;
		runtime.MaterialFrictionParams[i].nbMaterialFrictions = 0;

		runtime.SuspensionLimitParams[i].restitution = 0.0f;
		runtime.SuspensionLimitParams[i].directionForSuspensionLimitConstraint =
			vehicle2::PxVehiclePhysXSuspensionLimitConstraintParams::eROAD_GEOMETRY_NORMAL;
	}

	vehicle2::PxVehicleConstraintsCreate(runtime.Axles, *vehicle.World->Physics, *actor, runtime.PhysXConstraints);
	return true;
}

static bool BeginVehicleUpdate(Vehicle& vehicle)
{
	auto& runtime = vehicle.Runtime;
	auto wheelStates = VehicleArray(runtime.WheelRigidBodyStates);

	if (vehicle.Actor->getScene())
	{
		vehicle2::PxVehiclePhysxActorWakeup(
			runtime.Commands,
			&runtime.TransmissionCommands,
			&runtime.GearboxParams,
			&runtime.GearboxState,
			*vehicle.Actor,
			runtime.PhysXSteerState);

		if (vehicle2::PxVehiclePhysxActorSleepCheck(
			runtime.Axles,
			*vehicle.Actor,
			&runtime.EngineParams,
			runtime.RigidBodyState,
			runtime.PhysXConstraints,
			wheelStates,
			&runtime.EngineState))
		{
			return false;
		}
	}

	vehicle2::PxVehicleReadRigidBodyStateFromPhysXActor(*vehicle.Actor, runtime.RigidBodyState);
	return true;
}

static void UpdateCommandResponse(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	auto commands = runtime.Commands;
	auto transmissionCommands = runtime.TransmissionCommands;

	const PxReal longitudinalSpeed = runtime.RigidBodyState.getLongitudinalSpeed(context.frame);

	vehicle2::PxVehicleAutoBoxUpdate(
		runtime.EngineParams,
		runtime.GearboxParams,
		runtime.AutoboxParams,
		runtime.EngineState,
		runtime.GearboxState,
		dt,
		transmissionCommands.targetGear,
		runtime.AutoboxState,
		commands.throttle);

	vehicle2::PxVehicleSizedArrayData<const vehicle2::PxVehicleBrakeCommandResponseParams> brakeParams;
	brakeParams.setDataAndCount(runtime.BrakeResponseParams, 2);

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehicleBrakeCommandResponseUpdate(
			commands.brakes,
			commands.nbBrakes,
			longitudinalSpeed,
			wheelId,
			brakeParams,
			runtime.BrakeResponseStates[wheelId]);
	}

	vehicle2::PxVehicleGearCommandResponseUpdate(
		transmissionCommands.targetGear,
		runtime.GearboxParams,
		runtime.GearboxState);

	vehicle2::PxVehicleClutchCommandResponseLinearUpdate(
		transmissionCommands.clutch,
		runtime.ClutchCommandResponseParams,
		runtime.ClutchCommandResponseState);

	vehicle2::PxVehicleEngineDriveThrottleCommandResponseLinearUpdate(
		commands,
		runtime.ThrottleResponseState);

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehicleSteerCommandResponseUpdate(
			commands.steer,
			longitudinalSpeed,
			wheelId,
			runtime.SteerResponseParams,
			runtime.SteerResponseStates[wheelId]);
	}

	vehicle2::PxVehicleSizedArrayData<const vehicle2::PxVehicleAckermannParams> ackermannParams;
	ackermannParams.setDataAndCount(&runtime.AckermannParams, 1);

	auto steerResponseStates = VehicleArray(runtime.SteerResponseStates);

	vehicle2::PxVehicleAckermannSteerUpdate(
		commands.steer,
		runtime.SteerResponseParams,
		ackermannParams,
		steerResponseStates);
}

static void UpdateDifferential(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;

	vehicle2::PxVehicleDifferentialStateUpdate(
		runtime.Axles,
		runtime.DifferentialParams,
		VehicleConstArray(runtime.WheelRigidBodyStates),
		dt,
		runtime.DifferentialState,
		runtime.WheelConstraintGroupState);

	auto actuationStates = VehicleArray(runtime.ActuationStates);

	vehicle2::PxVehicleEngineDriveActuationStateUpdate(
		runtime.Axles,
		runtime.GearboxParams,
		VehicleConstArray(runtime.BrakeResponseStates),
		runtime.ThrottleResponseState,
		runtime.GearboxState,
		runtime.DifferentialState,
		runtime.ClutchCommandResponseState,
		actuationStates);
}

static void UpdateRoadGeometry(Vehicle& vehicle)
{
	auto& runtime = vehicle.Runtime;
	auto& context = vehicle.World->SimulationContext;

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehiclePhysXRoadGeometryQueryUpdate(
			runtime.WheelParams[wheelId],
			runtime.SuspensionParams[wheelId],
			runtime.RoadQueryParams.roadGeometryQueryType,
			runtime.RoadQueryParams.filterCallback,
			runtime.RoadQueryParams.defaultFilterData,
			runtime.MaterialFrictionParams[wheelId],
			runtime.SteerResponseStates[wheelId],
			runtime.RigidBodyState,
			*context.physxScene,
			context.physxUnitCylinderSweepMesh,
			context.frame,
			runtime.RoadGeometryStates[wheelId],
			nullptr);
	}
}

static void UpdateSuspension(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehicleSuspensionStateUpdate(
			runtime.WheelParams[wheelId],
			runtime.SuspensionParams[wheelId],
			runtime.SuspensionStateCalculationParams,
			runtime.SuspensionForceParams[wheelId].stiffness,
			runtime.SuspensionForceParams[wheelId].damping,
			runtime.SteerResponseStates[wheelId],
			runtime.RoadGeometryStates[wheelId],
			runtime.RigidBodyState,
			dt,
			context.frame,
			context.gravity,
			runtime.SuspensionStates[wheelId]);

		vehicle2::PxVehicleSuspensionComplianceUpdate(
			runtime.SuspensionParams[wheelId],
			runtime.SuspensionComplianceParams[wheelId],
			runtime.SuspensionStates[wheelId],
			runtime.SuspensionComplianceStates[wheelId]);

		vehicle2::PxVehicleSuspensionForceUpdate(
			runtime.SuspensionParams[wheelId],
			runtime.SuspensionForceParams[wheelId],
			runtime.RoadGeometryStates[wheelId],
			runtime.SuspensionStates[wheelId],
			runtime.SuspensionComplianceStates[wheelId],
			runtime.RigidBodyState,
			context.gravity,
			runtime.RigidBodyParams.mass,
			runtime.SuspensionForces[wheelId]);
	}
}

static void UpdateTires(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;
	auto actuationStates = VehicleConstArray(runtime.ActuationStates);

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehicleTireDirsUpdate(
			runtime.SuspensionParams[wheelId],
			runtime.SteerResponseStates[wheelId],
			runtime.RoadGeometryStates[wheelId].plane.n,
			runtime.RoadGeometryStates[wheelId].hitState,
			runtime.SuspensionComplianceStates[wheelId],
			runtime.RigidBodyState,
			context.frame,
			runtime.TireDirectionStates[wheelId]);

		vehicle2::PxVehicleTireSlipSpeedsUpdate(
			runtime.WheelParams[wheelId],
			runtime.SuspensionParams[wheelId],
			runtime.SteerResponseStates[wheelId],
			runtime.SuspensionStates[wheelId],
			runtime.TireDirectionStates[wheelId],
			runtime.RigidBodyState,
			runtime.RoadGeometryStates[wheelId],
			context.frame,
			runtime.TireSpeedStates[wheelId]);

		vehicle2::PxVehicleTireSlipsUpdate(
			runtime.WheelParams[wheelId],
			context.tireSlipParams,
			runtime.ActuationStates[wheelId],
			runtime.TireSpeedStates[wheelId],
			runtime.WheelRigidBodyStates[wheelId],
			runtime.TireSlipStates[wheelId]);

		vehicle2::PxVehicleTireCamberAnglesUpdate(
			runtime.SuspensionParams[wheelId],
			runtime.SteerResponseStates[wheelId],
			runtime.RoadGeometryStates[wheelId].plane.n,
			runtime.RoadGeometryStates[wheelId].hitState,
			runtime.SuspensionComplianceStates[wheelId],
			runtime.RigidBodyState,
			context.frame,
			runtime.TireCamberStates[wheelId]);

		vehicle2::PxVehicleTireGripUpdate(
			runtime.TireForceParams[wheelId],
			PxMax(runtime.RoadGeometryStates[wheelId].friction,
				runtime.MaterialFrictionParams[wheelId].defaultFriction),
			runtime.RoadGeometryStates[wheelId].hitState,
			runtime.SuspensionForces[wheelId],
			runtime.TireSlipStates[wheelId],
			runtime.TireGripStates[wheelId]);

		vehicle2::PxVehicleTireStickyStateUpdate(
			runtime.Axles,
			runtime.WheelParams[wheelId],
			context.tireStickyParams,
			actuationStates,
			runtime.TireGripStates[wheelId],
			runtime.TireSpeedStates[wheelId],
			runtime.WheelRigidBodyStates[wheelId],
			dt,
			runtime.TireStickyStates[wheelId]);

		vehicle2::PxVehicleTireSlipsAccountingForStickyStatesUpdate(
			runtime.TireStickyStates[wheelId],
			runtime.TireSlipStates[wheelId]);

		vehicle2::PxVehicleTireForcesUpdate(
			runtime.WheelParams[wheelId],
			runtime.SuspensionParams[wheelId],
			runtime.TireForceParams[wheelId],
			runtime.SuspensionComplianceStates[wheelId],
			runtime.TireGripStates[wheelId],
			runtime.TireDirectionStates[wheelId],
			runtime.TireSlipStates[wheelId],
			runtime.TireCamberStates[wheelId],
			runtime.RigidBodyState,
			runtime.TireForces[wheelId]);
	}
}

static void UpdateConstraints(Vehicle& vehicle)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	vehicle2::PxVehicleConstraintsDirtyStateUpdate(runtime.PhysXConstraints);

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehiclePhysXConstraintStatesUpdate(
			runtime.SuspensionParams[wheelId],
			runtime.SuspensionLimitParams[wheelId],
			runtime.SuspensionStates[wheelId],
			runtime.SuspensionComplianceStates[wheelId],
			runtime.RoadGeometryStates[wheelId].plane.n,
			context.tireStickyParams.stickyParams[vehicle2::PxVehicleTireDirectionModes::eLONGITUDINAL].damping,
			context.tireStickyParams.stickyParams[vehicle2::PxVehicleTireDirectionModes::eLATERAL].damping,
			runtime.TireDirectionStates[wheelId],
			runtime.TireStickyStates[wheelId],
			runtime.RigidBodyState,
			runtime.PhysXConstraints.constraintStates[wheelId]);
	}
}

static void UpdateDrivetrain(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;

	vehicle2::PxVehicleGearboxUpdate(runtime.GearboxParams, dt, runtime.GearboxState);

	auto wheelRigidBodyStates = VehicleArray(runtime.WheelRigidBodyStates);

	vehicle2::PxVehicleEngineDrivetrainUpdate(
		runtime.Axles,
		VehicleConstArray(runtime.WheelParams),
		runtime.EngineParams,
		runtime.ClutchParams,
		runtime.GearboxParams,
		VehicleConstArray(runtime.BrakeResponseStates),
		VehicleConstArray(runtime.ActuationStates),
		VehicleConstArray(runtime.TireForces),
		runtime.GearboxState,
		runtime.ThrottleResponseState,
		runtime.ClutchCommandResponseState,
		runtime.DifferentialState,
		&runtime.WheelConstraintGroupState,
		dt,
		wheelRigidBodyStates,
		runtime.EngineState,
		runtime.ClutchState);
}

static void UpdateRigidBody(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	vehicle2::PxVehicleRigidBodyUpdate(
		runtime.Axles,
		runtime.RigidBodyParams,
		VehicleConstArray(runtime.SuspensionForces),
		VehicleConstArray(runtime.TireForces),
		nullptr,
		dt,
		context.gravity,
		runtime.RigidBodyState);
}

static void UpdateWheels(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	for (PxU32 i = 0; i < runtime.Axles.nbWheels; i++)
	{
		const PxU32 wheelId = runtime.Axles.wheelIdsInAxleOrder[i];

		vehicle2::PxVehicleWheelRotationAngleUpdate(
			runtime.WheelParams[wheelId],
			runtime.ActuationStates[wheelId],
			runtime.SuspensionStates[wheelId],
			runtime.TireSpeedStates[wheelId],
			context.thresholdForwardSpeedForWheelAngleIntegration,
			dt,
			runtime.WheelRigidBodyStates[wheelId]);

		runtime.WheelLocalPoses[wheelId].localPose =
			vehicle2::PxVehicleComputeWheelLocalPose(
				context.frame,
				runtime.SuspensionParams[wheelId],
				runtime.SuspensionStates[wheelId],
				runtime.SuspensionComplianceStates[wheelId],
				runtime.SteerResponseStates[wheelId],
				runtime.WheelRigidBodyStates[wheelId]);
	}
}

static void EndVehicleUpdate(Vehicle& vehicle, float dt)
{
	auto& runtime = vehicle.Runtime;
	const auto& context = vehicle.World->SimulationContext;

	vehicle2::PxVehicleWriteRigidBodyStateToPhysXActor(
		context.physxActorUpdateMode,
		runtime.RigidBodyState,
		dt,
		*vehicle.Actor);

	vehicle2::PxVehiclePhysxActorKeepAwakeCheck(
		runtime.Axles,
		VehicleConstArray(runtime.WheelParams),
		VehicleConstArray(runtime.WheelRigidBodyStates),
		context.physxActorWakeCounterThreshold,
		context.physxActorWakeCounterResetValue,
		&runtime.GearboxState,
		nullptr,
		*vehicle.Actor);
}

static void SimulateVehicle(Vehicle& vehicle, float dt)
{
	if (!BeginVehicleUpdate(vehicle))
		return;

	UpdateCommandResponse(vehicle, dt);
	UpdateDifferential(vehicle, dt);
	UpdateRoadGeometry(vehicle);

	constexpr PxU32 substepCount = 3;
	const float substepDt = dt / substepCount;

	for (PxU32 i = 0; i < substepCount; i++)
	{
		UpdateSuspension(vehicle, substepDt);
		UpdateTires(vehicle, substepDt);
		UpdateConstraints(vehicle);
		UpdateDrivetrain(vehicle, substepDt);
		UpdateRigidBody(vehicle, substepDt);
	}

	UpdateWheels(vehicle, dt);
	EndVehicleUpdate(vehicle, dt);
}

static bool ConfigureSimpleVehicle(Vehicle& vehicle)
{
	auto& runtime = vehicle.Runtime;
	const auto& desc = vehicle.Desc;

	runtime.SetToDefault();

	const PxU32 frontIds[2] = { WheelFL, WheelFR };
	const PxU32 rearIds[2] = { WheelRL, WheelRR };

	runtime.Axles.addAxle(2, frontIds);
	runtime.Axles.addAxle(2, rearIds);


	runtime.SuspensionStateCalculationParams.suspensionJounceCalculationType =
		vehicle2::PxVehicleSuspensionJounceCalculationType::eSWEEP;
	runtime.SuspensionStateCalculationParams.limitSuspensionExpansionVelocity = false;

	ConfigureWheel(runtime, WheelFL, desc.FrontAxle.LeftWheel);
	ConfigureWheel(runtime, WheelFR, desc.FrontAxle.RightWheel);
	ConfigureWheel(runtime, WheelRL, desc.RearAxle.LeftWheel);
	ConfigureWheel(runtime, WheelRR, desc.RearAxle.RightWheel);

	const float frontZ =
		0.5f * (desc.FrontAxle.LeftWheel.Position.Z + desc.FrontAxle.RightWheel.Position.Z);

	const float rearZ =
		0.5f * (desc.RearAxle.LeftWheel.Position.Z + desc.RearAxle.RightWheel.Position.Z);

	const float wheelBase = std::abs(frontZ - rearZ);
	const float frontTrack = DistanceXZ(desc.FrontAxle.LeftWheel.Position, desc.FrontAxle.RightWheel.Position);

	const float bodyMass = vehicle.Actor->getMass();
	const float centerZ = vehicle.Actor->getCMassLocalPose().p.z;
	const float frontDistance = std::abs(frontZ - centerZ);
	const float rearDistance = std::abs(centerZ - rearZ);
	const float axleDistance = std::max(0.001f, frontDistance + rearDistance);

	const float frontAxleSprungMass = bodyMass * rearDistance / axleDistance;
	const float rearAxleSprungMass = bodyMass * frontDistance / axleDistance;

	const float gravityMagnitude = vehicle.World->SimulationContext.gravity.magnitude();

	const PxTransform cMassLocalPose = vehicle.Actor->getCMassLocalPose();

	ConfigureSuspension(runtime, WheelFL, desc.FrontAxle.LeftWheel, desc.FrontAxle, frontAxleSprungMass * 0.5f, gravityMagnitude, cMassLocalPose);
	ConfigureSuspension(runtime, WheelFR, desc.FrontAxle.RightWheel, desc.FrontAxle, frontAxleSprungMass * 0.5f, gravityMagnitude, cMassLocalPose);
	ConfigureSuspension(runtime, WheelRL, desc.RearAxle.LeftWheel, desc.RearAxle, rearAxleSprungMass * 0.5f, gravityMagnitude, cMassLocalPose);
	ConfigureSuspension(runtime, WheelRR, desc.RearAxle.RightWheel, desc.RearAxle, rearAxleSprungMass * 0.5f, gravityMagnitude, cMassLocalPose);

	ConfigureTire(runtime, WheelFL, frontAxleSprungMass * 0.5f, desc.FrontAxle.TireFriction);
	ConfigureTire(runtime, WheelFR, frontAxleSprungMass * 0.5f, desc.FrontAxle.TireFriction);
	ConfigureTire(runtime, WheelRL, rearAxleSprungMass * 0.5f, desc.RearAxle.TireFriction);
	ConfigureTire(runtime, WheelRR, rearAxleSprungMass * 0.5f, desc.RearAxle.TireFriction);

	runtime.RigidBodyParams.mass = bodyMass;
	runtime.RigidBodyParams.moi = vehicle.Actor->getMassSpaceInertiaTensor();

	runtime.SteerResponseParams.maxResponse = desc.MaxSteeringAngle;
	runtime.SteerResponseParams.wheelResponseMultipliers[WheelFL] = 1.0f;
	runtime.SteerResponseParams.wheelResponseMultipliers[WheelFR] = 1.0f;
	runtime.SteerResponseParams.wheelResponseMultipliers[WheelRL] = 0.0f;
	runtime.SteerResponseParams.wheelResponseMultipliers[WheelRR] = 0.0f;

	runtime.AckermannParams.wheelIds[0] = WheelFL;
	runtime.AckermannParams.wheelIds[1] = WheelFR;
	runtime.AckermannParams.wheelBase = wheelBase;
	runtime.AckermannParams.trackWidth = frontTrack;
	runtime.AckermannParams.strength = 1.0f;

	runtime.BrakeResponseParams[0].maxResponse = desc.MaxBrakeTorque;
	runtime.BrakeResponseParams[0].wheelResponseMultipliers[WheelFL] = 1.0f;
	runtime.BrakeResponseParams[0].wheelResponseMultipliers[WheelFR] = 1.0f;
	runtime.BrakeResponseParams[0].wheelResponseMultipliers[WheelRL] = 1.0f;
	runtime.BrakeResponseParams[0].wheelResponseMultipliers[WheelRR] = 1.0f;

	runtime.BrakeResponseParams[1].maxResponse = desc.MaxHandBrakeTorque;
	runtime.BrakeResponseParams[1].wheelResponseMultipliers[WheelFL] = 0.0f;
	runtime.BrakeResponseParams[1].wheelResponseMultipliers[WheelFR] = 0.0f;
	runtime.BrakeResponseParams[1].wheelResponseMultipliers[WheelRL] = 1.0f;
	runtime.BrakeResponseParams[1].wheelResponseMultipliers[WheelRR] = 1.0f;

	runtime.EngineParams.peakTorque = desc.MaxMotorTorque;
	runtime.EngineParams.idleOmega = RpmToOmega(desc.IdleMotorRpm);
	runtime.EngineParams.maxOmega = RpmToOmega(desc.MaxMotorRpm);
	runtime.EngineParams.moi = 1.0f;
	runtime.EngineParams.dampingRateFullThrottle = 0.15f;
	runtime.EngineParams.dampingRateZeroThrottleClutchEngaged = 2.0f;
	runtime.EngineParams.dampingRateZeroThrottleClutchDisengaged = 0.35f;
	runtime.EngineParams.torqueCurve.clear();
	runtime.EngineParams.torqueCurve.addPair(0.0f, 0.8f);
	runtime.EngineParams.torqueCurve.addPair(0.35f, 1.0f);
	runtime.EngineParams.torqueCurve.addPair(0.75f, 0.9f);
	runtime.EngineParams.torqueCurve.addPair(1.0f, 0.0f);

	runtime.GearboxParams.neutralGear = 1;
	runtime.GearboxParams.nbRatios = 7;
	runtime.GearboxParams.ratios[0] = -3.5f;
	runtime.GearboxParams.ratios[1] = 0.0f;
	runtime.GearboxParams.ratios[2] = 3.6f;
	runtime.GearboxParams.ratios[3] = 2.2f;
	runtime.GearboxParams.ratios[4] = 1.5f;
	runtime.GearboxParams.ratios[5] = 1.1f;
	runtime.GearboxParams.ratios[6] = 0.85f;
	runtime.GearboxParams.finalRatio = 3.7f;
	runtime.GearboxParams.switchTime = 0.35f;

	for (PxU32 i = 0; i < runtime.GearboxParams.nbRatios; i++)
	{
		runtime.AutoboxParams.upRatios[i] = 0.85f;
		runtime.AutoboxParams.downRatios[i] = 0.45f;
	}

	runtime.AutoboxParams.latency = 0.5f;

	runtime.ClutchCommandResponseParams.maxResponse = 10.0f;
	runtime.ClutchParams.accuracyMode = vehicle2::PxVehicleClutchAccuracyMode::eESTIMATE;
	runtime.ClutchParams.estimateIterations = 5;

	for (PxU32 i = 0; i < WheelCount; i++)
	{
		runtime.DifferentialParams.torqueRatios[i] = 0.0f;
		runtime.DifferentialParams.aveWheelSpeedRatios[i] = 0.0f;
	}

	if (desc.DriveType == VehicleDriveType_Front)
	{
		runtime.DifferentialParams.torqueRatios[WheelFL] = 0.5f;
		runtime.DifferentialParams.torqueRatios[WheelFR] = 0.5f;
		runtime.DifferentialParams.aveWheelSpeedRatios[WheelFL] = 0.5f;
		runtime.DifferentialParams.aveWheelSpeedRatios[WheelFR] = 0.5f;
	}
	else if (desc.DriveType == VehicleDriveType_Rear)
	{
		runtime.DifferentialParams.torqueRatios[WheelRL] = 0.5f;
		runtime.DifferentialParams.torqueRatios[WheelRR] = 0.5f;
		runtime.DifferentialParams.aveWheelSpeedRatios[WheelRL] = 0.5f;
		runtime.DifferentialParams.aveWheelSpeedRatios[WheelRR] = 0.5f;
	}
	else
	{
		for (PxU32 i = 0; i < WheelCount; i++)
		{
			runtime.DifferentialParams.torqueRatios[i] = 0.25f;
			runtime.DifferentialParams.aveWheelSpeedRatios[i] = 0.25f;
		}
	}

	runtime.DifferentialParams.frontWheelIds[0] = WheelFL;
	runtime.DifferentialParams.frontWheelIds[1] = WheelFR;
	runtime.DifferentialParams.rearWheelIds[0] = WheelRL;
	runtime.DifferentialParams.rearWheelIds[1] = WheelRR;

	runtime.DifferentialParams.frontBias = desc.DriveType == VehicleDriveType_Rear ? 0.0f : 1.3f;
	runtime.DifferentialParams.frontTarget = desc.DriveType == VehicleDriveType_Rear ? 0.0f : 1.29f;
	runtime.DifferentialParams.rearBias = desc.DriveType == VehicleDriveType_Front ? 0.0f : 1.3f;
	runtime.DifferentialParams.rearTarget = desc.DriveType == VehicleDriveType_Front ? 0.0f : 1.29f;
	runtime.DifferentialParams.centerBias = desc.DriveType == VehicleDriveType_All ? 1.3f : 0.0f;
	runtime.DifferentialParams.centerTarget = desc.DriveType == VehicleDriveType_All ? 1.29f : 0.0f;
	runtime.DifferentialParams.rate = 10.0f;

	runtime.EngineState.rotationSpeed = runtime.EngineParams.idleOmega;
	runtime.GearboxState.currentGear = runtime.GearboxParams.neutralGear;
	runtime.GearboxState.targetGear = runtime.GearboxParams.neutralGear;
	runtime.TransmissionCommands.targetGear = runtime.GearboxParams.neutralGear;
	return true;
}


VehicleWorld* VehicleWorldCreate(const VehicleWorldDesc* desc)
{
	if (!desc || !desc->Physics || !desc->Scene || !desc->DefaultMaterial)
		return nullptr;

	auto* physics = reinterpret_cast<PxPhysics*>(desc->Physics);
	auto* scene = reinterpret_cast<PxScene*>(desc->Scene);
	auto* material = reinterpret_cast<PxMaterial*>(desc->DefaultMaterial);

	return new (std::nothrow) VehicleWorld(physics, scene, material);
}

void VehicleWorldDestroy(VehicleWorld* world)
{
	delete world; 
}

Vehicle* VehicleCreateSimple(VehicleWorld* world, PxRigidDynamic* actor, const VehicleSimpleDesc* desc)
{
	if (!world || !actor || !desc || !ValidateSimpleDesc(*desc))
		return nullptr;

	if (actor->getScene() != world->Scene || actor->getMass() <= 0.0f)
		return nullptr;

	auto* vehicle = new (std::nothrow) Vehicle();
	if (!vehicle)
		return nullptr;

	vehicle->World = world;
	vehicle->Actor = actor;
	vehicle->Desc = *desc;
	vehicle->InitialPose = FromPx(actor->getGlobalPose());

	if (!ConfigureSimpleVehicle(*vehicle) || !ConfigurePhysXVehicle(*vehicle))
	{
		delete vehicle;
		return nullptr;
	}

	vehicle2::PxVehicleReadRigidBodyStateFromPhysXActor(*actor, vehicle->Runtime.RigidBodyState);

	vehicle->Initialized = true;
	return vehicle;
}

void VehicleDestroy(Vehicle* vehicle)
{
	if (!vehicle)
		return;

	if (vehicle->Initialized)
		vehicle2::PxVehicleConstraintsDestroy(vehicle->Runtime.PhysXConstraints);

	delete vehicle;
}

int32_t VehicleUpdate(Vehicle* vehicle, float deltaTime, const VehicleInput* input, VehicleState* state)
{
	if (!vehicle || !vehicle->Initialized || !input || !state || deltaTime <= 0.0f)
		return 0;

	auto& runtime = vehicle->Runtime;

	runtime.Commands.throttle = Clamp01(input->Throttle);
	runtime.Commands.steer = ClampSigned(input->Steering);
	runtime.Commands.brakes[0] = Clamp01(input->Brake);
	runtime.Commands.brakes[1] = Clamp01(input->HandBrake);
	runtime.Commands.nbBrakes = 2;
	runtime.TransmissionCommands.clutch = runtime.Commands.throttle > 0.0f ? 0.0f : 1.0f;

	vehicle->GearMode = input->GearMode;

	if (input->GearMode == VehicleGearMode_Automatic)
	{
		if (runtime.GearboxState.currentGear == runtime.GearboxParams.neutralGear &&
			runtime.Commands.throttle <= 0.0f)
		{
			runtime.TransmissionCommands.targetGear = runtime.GearboxParams.neutralGear;
		}
		else
		{
			runtime.TransmissionCommands.targetGear =
				vehicle2::PxVehicleEngineDriveTransmissionCommandState::eAUTOMATIC_GEAR;
		}
	}
	else
	{
		const int32_t neutral = static_cast<int32_t>(runtime.GearboxParams.neutralGear);
		const int32_t requested = input->Gear;

		if (requested < 0)
			runtime.TransmissionCommands.targetGear = 0;
		else if (requested == 0)
			runtime.TransmissionCommands.targetGear = neutral;
		else
			runtime.TransmissionCommands.targetGear =
			static_cast<PxU32>(std::min(
				neutral + requested,
				static_cast<int32_t>(runtime.GearboxParams.nbRatios - 1)));
	}

	SimulateVehicle(*vehicle, deltaTime);

	const PxTransform bodyPose = vehicle->Actor->getGlobalPose();

	state->BodyPose = FromPx(bodyPose);

	const PxTransform cMassLocalPose = vehicle->Actor->getCMassLocalPose();

	for (PxU32 i = 0; i < WheelCount; i++)
	{
		const PxTransform localPose = runtime.WheelLocalPoses[i].localPose;
		state->WheelPoses[i] = FromPx(bodyPose * cMassLocalPose * localPose);
	}

	state->SteeringAngle =
		0.5f * (
			runtime.SteerResponseStates[WheelFL] +
			runtime.SteerResponseStates[WheelFR]);

	const PxVec3 forward = bodyPose.q.rotate(PxVec3(0.0f, 0.0f, -1.0f));
	state->Speed = vehicle->Actor->getLinearVelocity().dot(forward);
	state->MotorRpm = OmegaToRpm(runtime.EngineState.rotationSpeed);

	const int32_t neutral = static_cast<int32_t>(runtime.GearboxParams.neutralGear);
	const int32_t currentGear = static_cast<int32_t>(runtime.GearboxState.currentGear);

	if (currentGear < neutral)
		state->Gear = -1;
	else if (currentGear == neutral)
		state->Gear = 0;
	else
		state->Gear = currentGear - neutral;

	state->GearMode = vehicle->GearMode;

	return 1;
}

void VehicleSetPose(Vehicle* vehicle, const Pose3f* pose)
{
	if (!vehicle || !vehicle->Actor || !pose)
		return;

	vehicle->Actor->setGlobalPose(ToPx(*pose));
	vehicle->Actor->setLinearVelocity(PxVec3(0.0f));
	vehicle->Actor->setAngularVelocity(PxVec3(0.0f));

	vehicle->Runtime.RigidBodyState = {};
	vehicle->Runtime.RigidBodyState.setToDefault();
	vehicle->Runtime.RigidBodyState.pose = ToPx(*pose);

	for (PxU32 i = 0; i < WheelCount; i++)
	{
		vehicle->Runtime.WheelRigidBodyStates[i].setToDefault();
		vehicle->Runtime.WheelLocalPoses[i].setToDefault();
		vehicle->Runtime.SuspensionStates[i].setToDefault();
	}
}

void VehicleReset(Vehicle* vehicle)
{
	if (!vehicle)
		return;

	VehicleSetPose(vehicle, &vehicle->InitialPose);

	vehicle->Runtime.EngineState.setToDefault();
	vehicle->Runtime.EngineState.rotationSpeed = vehicle->Runtime.EngineParams.idleOmega;
	vehicle->Runtime.GearboxState.setToDefault();
	vehicle->Runtime.AutoboxState.setToDefault();
	vehicle->Runtime.ClutchCommandResponseState.setToDefault();
	vehicle->Runtime.ClutchState.setToDefault();
	vehicle->Runtime.DifferentialState.setToDefault();
	vehicle->Runtime.WheelConstraintGroupState.setToDefault();

	vehicle->Runtime.GearboxState.currentGear =
		vehicle->Runtime.GearboxParams.neutralGear;

	vehicle->Runtime.GearboxState.targetGear =
		vehicle->Runtime.GearboxParams.neutralGear;

	vehicle->Runtime.TransmissionCommands.targetGear =
		vehicle->Runtime.GearboxParams.neutralGear;

	vehicle->GearMode = VehicleGearMode_Automatic;
}
