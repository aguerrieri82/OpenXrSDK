﻿using MagicPhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;

namespace Xr.Engine.Physics
{
    public enum PhysicsActorType
    {
        Static,
        Dynamic
    }

    public struct PhysicsActorInfo
    {
        public PhysicsActorType Type;

        public PhysicsShape Shape;

        public PxTransform Transform;

        public float Density;
    }


    public unsafe struct PhysicsActor
    {
        PxActor* _handle;

        public PhysicsActor(PxActor* handler)
        {
            _handle = handler;
        }

        public float Mass
        {
            get => RigidBody.GetMass();
        }

        public PxTransform GlobalPose
        {
            get => RigidActor.GetGlobalPose();
            set => RigidActor.SetGlobalPoseMut(&value, true);
        }

        public PxTransform KinematicTarget
        {
            get 
            {
                PxTransform value;
                RigidDynamic.GetKinematicTarget(&value);
                return value;
            }
            set => RigidDynamic.SetKinematicTargetMut(&value);
        }


        public bool IsKinematic
        {
            get => (RigidBody.GetRigidBodyFlags() & PxRigidBodyFlags.Kinematic) == PxRigidBodyFlags.Kinematic;
            set => RigidBody.SetRigidBodyFlagMut(PxRigidBodyFlag.Kinematic, value);
        }

        public void Release()
        {
            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
        }

        public bool IsValid => _handle != null;

        public readonly ref PxRigidDynamic RigidDynamic => ref Unsafe.AsRef<PxRigidDynamic>(_handle);

        public readonly ref PxRigidStatic RigidStatic => ref Unsafe.AsRef<PxRigidStatic>(_handle);

        public readonly ref PxRigidBody RigidBody => ref Unsafe.AsRef<PxRigidBody>(_handle);
        
        public readonly ref PxRigidActor RigidActor => ref Unsafe.AsRef<PxRigidActor>(_handle);

        public readonly ref PxActor Actor => ref Unsafe.AsRef<PxActor>(_handle);


        public static implicit operator PxActor*(PhysicsActor self) => self._handle;
    }
}
