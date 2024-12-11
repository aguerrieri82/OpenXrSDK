using System.Runtime.CompilerServices;

namespace PhysX.Framework
{


    public unsafe class PhysicsConstraint : PhysicsObject<PxConstraint>
    {
        public PhysicsConstraint(PxConstraint* handle, PhysicsSystem system) : base(handle, system)
        {
        }

        public unsafe void GetActors(out PhysicsActor actor, out PhysicsActor otherActor)
        {
            PxRigidActor* a1;
            PxRigidActor* a2;

            _handle->GetActors(&a1, &a2);
            actor = _system.GetObject<PhysicsRigidActor>(a1);
            otherActor = _system.GetObject<PhysicsRigidActor>(a2);
        }

        public void MarkDirty()
        {
            _handle->MarkDirtyMut();
        }

        public bool IsValid => _handle->IsValid();

        public VelocityVector Force
        {
            get
            {
                VelocityVector result;
                _handle->GetForce((PxVec3*)&result.Linear, (PxVec3*)&result.Angular);
                return result;
            }
        }

        public VelocityModule BreakForce
        {
            get
            {
                VelocityModule result;
                _handle->GetBreakForce(&result.Linear, &result.Angular);
                return result;
            }
            set
            {
                _handle->SetBreakForceMut(value.Linear, value.Angular);
            }
        }

        public float MinResponseThreshold
        {
            get => _handle->GetMinResponseThreshold();
            set => _handle->SetMinResponseThresholdMut(value);
        }

        public PxConstraintFlags Flags
        {
            get => _handle->GetFlags();
            set => _handle->SetFlagsMut(value);
        }

        public override void Dispose()
        {
            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
        }

        public ref PxConstraint Constraint => ref Unsafe.AsRef<PxConstraint>(_handle);
    }
}
