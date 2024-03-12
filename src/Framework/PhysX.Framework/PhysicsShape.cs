using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using XrMath;

namespace PhysX.Framework
{
    public struct PhysicsShapeInfo
    {
        public PhysicsShapeInfo()
        {
            IsEsclusive = true;
            Flags = PxShapeFlags.Visualization | PxShapeFlags.SceneQueryShape | PxShapeFlags.SimulationShape;
        }

        public PhysicsGeometry? Geometry;

        public PhysicsMaterial? Material;

        public bool IsEsclusive;

        public PxShapeFlags Flags;
    }

    public unsafe class PhysicsShape : PhysicsObject<PxShape>
    {

        protected string _name;
        protected PhysicsGeometry _geometry;

        internal PhysicsShape(PxShape* handle, PhysicsGeometry geometry, PhysicsSystem system)
    : base(handle, system)
        {
            _name = string.Empty;
            _geometry = geometry;
        }

        public unsafe RaycastHit[] Raycast(PhysicsActor owner, Ray3 ray, float maxDistance, PxHitFlags flags, int maxHits)
        {
            var result = new PxRaycastHit[maxHits];
            uint count;

            fixed (PxRaycastHit* pResult = result)
                count = _handle->ExtRaycast((PxRigidActor*)owner.Handle, (PxVec3*)&ray.Origin, (PxVec3*)&ray.Direction, maxDistance, flags, (uint)maxHits, pResult);

            var newResults = new RaycastHit[count];

            for (var i = 0; i < count; i++)
            {
                newResults[i] = new RaycastHit
                {
                    Actor = _system.GetObject<PhysicsActor>(result[i].actor),
                    Shape = _system.GetObject<PhysicsShape>(result[i].shape),
                    Normal = result[i].normal,
                    Position = result[i].position,
                    UV = new Vector2(result[i].u, result[i].v),
                    Distance = result[i].distance
                };
            }

            return newResults;
        }

        public unsafe Pose3 GlobalPose(PhysicsActor actor)
        {
            return _handle->ExtGetGlobalPose((PxRigidActor*)actor.Handle).ToPose3();
        }

        public override void Dispose()
        {
            _system.DeleteObject(this);

            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }

            GC.SuppressFinalize(this);
        }

        public Bounds3 WorldBounds(PhysicsActor actor)
        {
            var bounds = _handle->ExtGetWorldBounds((PxRigidActor*)actor.Handle, 0);
            return new Bounds3
            {
                Min = bounds.minimum,
                Max = bounds.maximum,
            };
        }

        public unsafe void SetMaterials(IList<PhysicsMaterial> materials)
        {
            var handles = stackalloc PxMaterial*[materials.Count];
            for (var i = 0; i < materials.Count; i++)
                handles[i] = materials[i];

            _handle->SetMaterialsMut(handles, (ushort)materials.Count);
        }

        public unsafe PhysicsMaterial[] GetMaterials()
        {
            var size = _handle->GetNbMaterials();

            var materials = stackalloc PxMaterial*[size];

            _handle->GetMaterials(materials, size, 0);

            var result = new PhysicsMaterial[size];

            for (var i = 0; i < size; i++)
                result[i] = _system.GetObject<PhysicsMaterial>(materials[i]);

            return result;
        }

        public string Name
        {
            get => _name;
            set
            {
                var data = Encoding.UTF8.GetBytes(value);
                fixed (byte* pData = data)
                    _handle->SetNameMut(pData);
                _name = value;
            }
        }

        public PxShapeFlags Flags
        {
            get => _handle->GetFlags();
            set => _handle->SetFlagsMut(value);
        }

        public Pose3 LocalPose
        {
            get => _handle->GetLocalPose().ToPose3();
            set
            {
                var newValue = value.ToPxTransform();
                _handle->SetLocalPoseMut(&newValue);
            }
        }

        public float ResetOffset
        {
            get => _handle->GetRestOffset();
            set => _handle->SetRestOffsetMut(value);
        }


        public float ContactOffset
        {
            get => _handle->GetContactOffset();
            set => _handle->SetContactOffsetMut(value);
        }


        public float TorsionalPatchRadius
        {
            get => _handle->GetTorsionalPatchRadius();
            set => _handle->SetTorsionalPatchRadiusMut(value);
        }

        public float MinTorsionalPatchRadius
        {
            get => _handle->GetMinTorsionalPatchRadius();
            set => _handle->SetMinTorsionalPatchRadiusMut(value);
        }

        public float DensityForFluid
        {
            get => _handle->GetDensityForFluid();
            set => _handle->SetDensityForFluidMut(value);
        }

        public PxFilterData SimulationFilterData
        {
            get => _handle->GetSimulationFilterData();
            set => _handle->SetSimulationFilterDataMut(&value);
        }

        public PxFilterData QueryFilterData
        {
            get => _handle->GetQueryFilterData();
            set => _handle->SetQueryFilterDataMut(&value);
        }

        public PhysicsGeometry Geometry
        {
            get => _geometry;
            set
            {
                _geometry = value;
                _handle->SetGeometryMut(value);
            }
        }

        public bool IsExclusive => _handle->IsExclusive();

        public ref PxShape Shape => ref Unsafe.AsRef<PxShape>(_handle);
    }
}
