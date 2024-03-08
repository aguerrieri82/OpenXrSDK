using System.Numerics;
using System.Text;
using MagicPhysX;
using static MagicPhysX.NativeMethods;
using System.Runtime.CompilerServices;

namespace Xr.Engine.Physics
{
    public unsafe class PhysicsSystem
    {
        const uint VersionNumber = (5 << 24) + (1 << 16) + (3 << 8);

        private PxFoundation* _foundation;
        private PxPvd* _pvd;
        private PxPhysics* _physics;
        private PxDefaultCpuDispatcher* _dispatcher;
        private PxScene* _scene;

        public PhysicsSystem() 
        {
        }

        public PhysicsMaterial CreateMaterial(PhysicsMaterialInfo info)
        {
            var material = _physics->CreateMaterialMut(info.StaticFriction, info.DynamicFriction, info.Restitution);
            return new PhysicsMaterial(material);
        }

        public PhysicsGeometry CreateBox(Vector3 halfSize)
        {
            var geo = PxBoxGeometry_new_1(halfSize);
     
            return new PhysicsGeometry((PxGeometry*)&geo, PhysicsGeometryType.Box);
        }

        public PhysicsGeometry CreateSphere(float radius)
        {
            var geo = PxSphereGeometry_new(radius);

            return new PhysicsGeometry((PxGeometry*)&geo, PhysicsGeometryType.Sphere);
        }

        public PhysicsGeometry CreateCapsule(float radius, float height)
        {
            var geo = PxCapsuleGeometry_new(radius, height);

            return new PhysicsGeometry((PxGeometry*)&geo, PhysicsGeometryType.Capsule);
        }

        public PhysicsGeometry CreateTriangleMesh(uint[] indices, Vector3[] vertices, Vector3 scale)
        {
            PxTriangleMeshCookingResult result;

            var desc = new PxTriangleMeshDesc();
            desc.points.data = Unsafe.AsPointer(ref vertices[0]);
            desc.points.stride = 12;
            desc.points.count = (uint)vertices.Length;
            desc.triangles.count = (uint)indices.Length / 3;
            desc.triangles.data = Unsafe.AsPointer(ref indices[0]);
            desc.triangles.stride = 12;

            var tolScale = new PxTolerancesScale
            {
                length = 1,
                speed = 10
            };


            var param = tolScale.CookingParamsNew();

            /*
            var allocator = phys_PxGetAllocatorCallback();
            var outStream = allocator->DefaultMemoryOutputStreamNewAlloc();

            param.PhysPxCookTriangleMesh(&desc, (PxOutputStream*)outStream, &result);

            allocator->Delete();
            outStream->Delete();
            */

            var mesh =  param.PhysPxCreateTriangleMesh(&desc, phys_PxGetStandaloneInsertionCallback(), &result);

            var meshScale = new PxMeshScale
            {
                scale = scale
            };

            var geo = PxTriangleMeshGeometry_new(mesh, &meshScale, 0);

            return new PhysicsGeometry((PxGeometry*)&geo, PhysicsGeometryType.TriangleMesh);
        }

        public PhysicsGeometry? CreateConvexMesh(uint[] indices, Vector3[] vertices, Vector3 scale)
        {
            PxConvexMeshCookingResult result;

            var desc = new PxConvexMeshDesc();
            desc.points.data = Unsafe.AsPointer(ref vertices[0]);
            desc.points.stride = 12;
            desc.points.count = (uint)vertices.Length;
            desc.indices.data = Unsafe.AsPointer(ref indices[0]);
            desc.indices.count = (uint)indices.Length;
            desc.indices.stride = 4;
            desc.quantizedCount = 10;
            desc.vertexLimit = 200;
            desc.polygonLimit = 10;
            desc.flags = PxConvexFlags.ComputeConvex | PxConvexFlags.DisableMeshValidation | PxConvexFlags.FastInertiaComputation;

            var tolScale = new PxTolerancesScale
            {
                length = 1,
                speed = 10
            };

            var param = tolScale.CookingParamsNew();

            /*
            var allocator = phys_PxGetAllocatorCallback();
            var outStream = allocator->DefaultMemoryOutputStreamNewAlloc();

            param.PhysPxCookTriangleMesh(&desc, (PxOutputStream*)outStream, &result);

            allocator->Delete();
            outStream->Delete();
            */

            var isValid = param.PhysPxValidateConvexMesh(&desc);
            if (!isValid)
                return null;

            var mesh = param.PhysPxCreateConvexMesh(&desc, phys_PxGetStandaloneInsertionCallback(), &result);

            var meshScale = new PxMeshScale
            {
                scale = scale
            };

            var geo = PxConvexMeshGeometry_new(mesh, &meshScale, 0);

            return new PhysicsGeometry((PxGeometry*)&geo, PhysicsGeometryType.ConvexMesh);
        }

        public PhysicsShape CreateShape(PhysicsShapeInfo info)
        {
            var shape = _physics->CreateShapeMut(info.Geometry, info.Material, info.IsEsclusive, info.Flags);
            return new PhysicsShape(shape);
        }

        public PhysicsActor CreateActor(PhysicsActorInfo info)
        {
            PxActor* actor = null;

            switch (info.Type)
            {
                case PhysicsActorType.Static:
                    actor = (PxActor*)_physics->PhysPxCreateStatic1(&info.Transform, info.Shapes[0]);
                    break;
                case PhysicsActorType.Dynamic:
                case PhysicsActorType.Kinematic:
                    actor = (PxActor*)_physics->PhysPxCreateDynamic1(&info.Transform, info.Shapes[0], info.Density);
                    break;
            }

            foreach (var shape in info.Shapes.Skip(1))
                ((PxRigidActor*)actor)->AttachShapeMut(shape);

            ((PxRigidBody*)actor)->ExtUpdateMassAndInertia1(info.Density, null, true);

            _scene->AddActorMut(actor, null);

            var result = new PhysicsActor(actor);
            if (info.Type == PhysicsActorType.Kinematic)
                result.IsKinematic = true;
            return result;
        }

        public void CreateScene(Vector3 gravity)
        {
            var sceneDesc = PxSceneDesc_new(_physics->GetTolerancesScale());

            sceneDesc.gravity = gravity;
            sceneDesc.cpuDispatcher = (PxCpuDispatcher*)_dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();
            sceneDesc.solverType = PxSolverType.Pgs;

            _scene = _physics->CreateSceneMut(&sceneDesc);

            if (_pvd != null)
            {
                var pvdClient = _scene->GetScenePvdClientMut();

                if (pvdClient != null)
                {
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitConstraints, true);
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitContacts, true);
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitScenequeries, true);
                }
            }
        }

        public void Create()
        {
            _foundation = physx_create_foundation();

            _physics = _foundation->PhysxCreatePhysics();

            _dispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
        }

        public void Create(string host, int port)
        {
            _foundation = physx_create_foundation();

            _pvd = _foundation->PhysPxCreatePvd();

            fixed (byte* bytePointer = Encoding.UTF8.GetBytes(host))
            {
                var transport = phys_PxDefaultPvdSocketTransportCreate(bytePointer, port, 10000);
                _pvd->ConnectMut(transport, PxPvdInstrumentationFlags.All);
            }

            var tolerancesScale = new PxTolerancesScale
            {
                length = 1,
                speed = 10
            };

            _physics = phys_PxCreatePhysics(
                VersionNumber,
                _foundation,
                &tolerancesScale,
                true,
                _pvd,
                null);

            _physics->PhysPxInitExtensions(_pvd);

            _dispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
        }

        public void Simulate(TimeSpan delta, float stepSize)
        {
            Simulate((float)delta.TotalSeconds, stepSize);
        }

        public void Simulate(float deltaSecs, float stepSize)
        {
            uint error;
            float curTime = 0;
            while (curTime < deltaSecs)
            {
                if (curTime + stepSize > deltaSecs)
                    stepSize = deltaSecs - curTime;

                _scene->SimulateMut(stepSize, null, null, 0, true);

                curTime += stepSize;    
            }

            _scene->FetchResultsMut(true, &error);
        }

        public ref PxPhysics Physics => ref Unsafe.AsRef<PxPhysics>(_physics);

        public ref PxScene Scene => ref Unsafe.AsRef<PxScene>(_scene);
    }
}
