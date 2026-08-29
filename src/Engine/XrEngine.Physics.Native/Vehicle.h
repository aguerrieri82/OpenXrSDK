#pragma once

#ifdef _WINDOWS

	#define EXPORT __declspec(dllexport)

#else

	#define EXPORT __attribute__((visibility("default")))

	#define APIENTRY

#endif


typedef struct Vec3f
{
	float X;
	float Y;
	float Z;
} Vec3f;

typedef struct Quatf
{
	float X;
	float Y;
	float Z;
	float W;
} Quatf;

typedef struct Pose3f
{
	Vec3f Position;
	Quatf Orientation;
} Pose3f;

typedef enum VehicleGearMode
{
	VehicleGearMode_Automatic = 0,
	VehicleGearMode_Manual = 1
} VehicleGearMode;

typedef enum VehicleDriveType
{
	VehicleDriveType_Front = 0,
	VehicleDriveType_Rear = 1,
	VehicleDriveType_All = 2
} VehicleDriveType;

typedef struct VehicleWheelDesc
{
	Vec3f Position;

	float Radius;
	float Width;
	float Mass;
} VehicleWheelDesc;

typedef struct VehicleAxleSimpleDesc
{
	VehicleWheelDesc LeftWheel;
	VehicleWheelDesc RightWheel;

	float SuspensionTravel;
	float SuspensionStiffness;
	float SuspensionDamping;

	float TireFriction;
} VehicleAxleSimpleDesc;

typedef struct VehicleSimpleDesc
{
	VehicleAxleSimpleDesc FrontAxle;
	VehicleAxleSimpleDesc RearAxle;

	float MaxSteeringAngle;

	VehicleDriveType DriveType;

	float MaxMotorTorque;
	float MaxBrakeTorque;
	float MaxHandBrakeTorque;

	float IdleMotorRpm;
	float MaxMotorRpm;
} VehicleSimpleDesc;

typedef struct VehicleInput
{
	float Throttle;
	float Brake;
	float Steering;
	float HandBrake;

	VehicleGearMode GearMode;
	int32_t Gear;
} VehicleInput;

typedef struct VehicleState
{
	Pose3f BodyPose;
	Pose3f WheelPoses[4];

	float SteeringAngle;

	float Speed;
	float MotorRpm;

	int32_t Gear;
	VehicleGearMode GearMode;
} VehicleState;

typedef struct VehicleWorld VehicleWorld;
typedef struct Vehicle Vehicle;

typedef struct VehicleWorldDesc
{
	physx::PxPhysics* Physics;
	physx::PxScene* Scene;
	physx::PxMaterial* DefaultMaterial;
} VehicleWorldDesc;


extern "C"
{
	EXPORT VehicleWorld* APIENTRY VehicleWorldCreate(const VehicleWorldDesc* desc);
	
	EXPORT void APIENTRY VehicleWorldDestroy(VehicleWorld* world);

	EXPORT Vehicle* APIENTRY VehicleCreateSimple(VehicleWorld* world, physx::PxRigidDynamic* actor, const VehicleSimpleDesc* desc);

	EXPORT void APIENTRY VehicleDestroy(Vehicle* vehicle);
	
	EXPORT int32_t APIENTRY VehicleUpdate(Vehicle* vehicle, float deltaTime, const VehicleInput* input, VehicleState* state);
	
	EXPORT void APIENTRY VehicleSetPose(Vehicle* vehicle, const Pose3f* pose);
	
	EXPORT void APIENTRY VehicleReset(Vehicle* vehicle);
}