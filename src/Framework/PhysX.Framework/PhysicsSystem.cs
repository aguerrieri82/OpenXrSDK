using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PhysX.NativeMethods;


namespace PhysX.Framework
{
    internal unsafe partial struct PxContactPairVelocity2
    {
        public readonly byte type_;
        public fixed byte structgen_pad0[3];
        public PxVec3 linearVelocity1;
        public PxVec3 linearVelocity2;
        public PxVec3 angularVelocity1;
        public PxVec3 angularVelocity2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct PxContactPairPose2
    {
        public byte type_;
        public fixed byte structgen_pad0[3];
        public PxTransform globalPose1;
        public PxTransform globalPose2;
    }


    public class PhysicsOptions
    {
        public PhysicsOptions()
        {
            LengthTolerance = 1;
            SpeedTolerance = 4;
            DebugHost = "localhost";
            DebugPort = 5425;
            UseDebug = true;
            Gravity = new Vector3(0, -9.81f, 0);
        }

        public ILogger? Logger { get; set; }

        public bool UseDebug { get; set; }

        public string? DebugHost { get; set; }

        public int DebugPort { get; set; }

        public float LengthTolerance { get; set; }

        public float SpeedTolerance { get; set; }

        public Vector3 Gravity { get; set; }
    }

    public unsafe class PhysicsSystem : IDisposable
    {
        const uint VersionNumber = (5 << 24) + (1 << 16) + (3 << 8);

        protected PxFoundation* _foundation;
        protected PxPvd* _pvd;
        protected PxTolerancesScale _tolerancesScale;
        protected PxPhysics* _physics;
        protected PxDefaultCpuDispatcher* _dispatcher;
        protected PhysicsScene? _scene;

        protected ILogger? _logger;

        protected uint _actorIds;
        protected Dictionary<uint, PhysicsActor> _actors = [];
        protected Dictionary<nint, object> _objects = [];
        protected IList<PhysicsMaterial> _materials = [];

        protected SimulationEventCallbacks _eventCallbacks;
        protected ContactModifyCallback _contactModify;

        protected float _lastDeltaTime;

        public PhysicsSystem()
        {
            Current = this;
            _contactModify = new ContactModifyCallback();
            _eventCallbacks = new SimulationEventCallbacks();
        }

        public PhysicsMaterial CreateOrGetMaterial(PhysicsMaterialInfo info)
        {
            var result = _materials.FirstOrDefault(a =>
                a.StaticFriction == info.StaticFriction &&
                a.DynamicFriction == info.DynamicFriction &&
                a.Restitution == info.Restitution);

            if (result != null)
                result.AddRef();
            else
                result = CreateMaterial(info, true);

            return result;
        }

        public PhysicsMaterial CreateMaterial(PhysicsMaterialInfo info, bool shared = false)
        {
            var material = _physics->CreateMaterialMut(info.StaticFriction, info.DynamicFriction, info.Restitution);
            var result = new PhysicsMaterial(material, this);
            _objects[new nint(material)] = result;
            if (shared)
                _materials.Add(result);
            return result;
        }

        public PhysicsGeometry CreateBox(Vector3 halfSize)
        {
            var geo = PxBoxGeometry_new_1(halfSize);

            return new PhysicsGeometry((PxGeometry*)&geo);
        }

        public PhysicsGeometry CreateSphere(float radius)
        {
            var geo = PxSphereGeometry_new(radius);

            return new PhysicsGeometry((PxGeometry*)&geo);
        }

        public PhysicsGeometry CreateCapsule(float radius, float height)
        {
            var geo = PxCapsuleGeometry_new(radius, height);

            return new PhysicsGeometry((PxGeometry*)&geo);
        }

        public PhysicsGeometry CreateTriangleMesh(uint[] indices, Vector3[] vertices, Vector3 scale, float tolerance)
        {
            PxTriangleMeshCookingResult result;

            var desc = new PxTriangleMeshDesc();
            desc.points.data = Unsafe.AsPointer(ref vertices[0]);
            desc.points.stride = 12;
            desc.points.count = (uint)vertices.Length;
            desc.triangles.count = (uint)indices.Length / 3;
            desc.triangles.data = Unsafe.AsPointer(ref indices[0]);
            desc.triangles.stride = 12;

            var curScale = new PxTolerancesScale()
            {
                length = tolerance,
                speed = _tolerancesScale.speed
            };

            var param = curScale.CookingParamsNew();

            /*
            var allocator = phys_PxGetAllocatorCallback();
            var outStream = allocator->DefaultMemoryOutputStreamNewAlloc();

            param.PhysPxCookTriangleMesh(&desc, (PxOutputStream*)outStream, &result);

            allocator->Delete();
            outStream->Delete();
            */

            var mesh = param.PhysPxCreateTriangleMesh(&desc, phys_PxGetStandaloneInsertionCallback(), &result);

            var meshScale = new PxMeshScale
            {
                scale = scale
            };

            var geo = PxTriangleMeshGeometry_new(mesh, &meshScale, 0);

            return new PhysicsGeometry((PxGeometry*)&geo);
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

            var curScale = _tolerancesScale;

            var param = curScale.CookingParamsNew();

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

            return new PhysicsGeometry((PxGeometry*)&geo);
        }

        public PhysicsShape CreateShape(PhysicsShapeInfo info)
        {
            var shape = _physics->CreateShapeMut(info.Geometry!, info.Material!, info.IsEsclusive, info.Flags);
            var result = new PhysicsShape(shape, info.Geometry!, this);
            _objects[new nint(shape)] = result;
            return result;
        }

        public PhysicsActor CreateActor(PhysicsActorInfo info)
        {
            PxActor* actor = null;
            PhysicsActor? result;
            var pxTrans = info.Pose.ToPxTransform();

            switch (info.Type)
            {
                case PhysicsActorType.Static:
                    actor = (PxActor*)_physics->PhysPxCreateStatic1(&pxTrans, info.Shapes[0]);
                    result = new PhysicsStaticActor(actor, this);
                    break;
                case PhysicsActorType.Dynamic:
                case PhysicsActorType.Kinematic:
                    actor = (PxActor*)_physics->PhysPxCreateDynamic1(&pxTrans, info.Shapes[0], info.Density);
                    result = new PhysicsDynamicActor(actor, this);
                    break;
                default:
                    throw new NotSupportedException();
            }

            foreach (var shape in info.Shapes.Skip(1))
                result.AddShape(shape);

            /*
            if (info.Type != PhysicsActorType.Static)
                ((PxRigidBody*)actor)->ExtUpdateMassAndInertia1(info.Density, null, true);
            */

            _scene!.AddActor(result);

            if (info.Type == PhysicsActorType.Kinematic)
                ((PhysicsDynamicActor)result).IsKinematic = true;

            result.Id = _actorIds++;

            _actors[result.Id] = result;

            foreach (var shape in info.Shapes)
            {
                PxFilterData data;
                data.word0 = result.Id;
                shape.Shape.SetSimulationFilterDataMut(&data);
            }

            _objects[new nint(actor)] = result;

            return result;
        }

        protected virtual internal void NotifyContact(PxContactPairHeader2 header)
        {
            var actor1 = (PhysicsActor)_objects[new nint(header.actor1)];
            var actor2 = (PhysicsActor)_objects[new nint(header.actor2)];

            var pairs = new ContactPair[header.nbPairs];

            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = header.pairs[i];

                var newPair = new ContactPair();
                newPair.Item0.Shape = GetObject<PhysicsShape>(pair.shape1);
                newPair.Item1.Shape = GetObject<PhysicsShape>(pair.shape2);
                newPair.Points = pair.contactCount > 0 ? new PhysicsContactPoint[pair.contactCount] : [];

                if (pair.contactCount > 0)
                {
                    var points = new PxContactPairPoint[pair.contactCount];

                    fixed (PxContactPairPoint* pBuffer = points)
                        PxContactPair_extractContacts((PxContactPair*)&pair, pBuffer, (uint)points.Length);

                    for (var j = 0; j < pair.contactCount; j++)
                    {
                        newPair.Points[j] = new PhysicsContactPoint
                        {
                            Position = points[j].position,
                            Normal = points[j].normal,
                            Separation = points[j].separation,
                            Impulse = points[j].impulse,
                        };
                    }
                }

                pairs[i] = newPair;
            }

            if (header.extraDataStreamSize > 0)
            {
                var iterator = PxContactPairExtraDataIterator_new(header.extraDataStream, header.extraDataStreamSize);

                while (iterator.NextItemSetMut())
                {
                    if (iterator.postSolverVelocity != null)
                    {
                        var vel = *(PxContactPairVelocity2*)iterator.postSolverVelocity;
                        pairs[iterator.contactPairIndex].Item0.PostVelocity.Linear = vel.linearVelocity1;
                        pairs[iterator.contactPairIndex].Item0.PostVelocity.Angular = vel.angularVelocity1;
                        pairs[iterator.contactPairIndex].Item1.PostVelocity.Linear = vel.linearVelocity2;
                        pairs[iterator.contactPairIndex].Item1.PostVelocity.Angular = vel.angularVelocity2;
                    }

                    if (iterator.preSolverVelocity != null)
                    {
                        var vel = *(PxContactPairVelocity2*)iterator.preSolverVelocity;
                        pairs[iterator.contactPairIndex].Item0.PreVelocity.Linear = vel.linearVelocity1;
                        pairs[iterator.contactPairIndex].Item0.PreVelocity.Angular = vel.angularVelocity1;
                        pairs[iterator.contactPairIndex].Item1.PreVelocity.Linear = vel.linearVelocity2;
                        pairs[iterator.contactPairIndex].Item1.PreVelocity.Angular = vel.angularVelocity2;
                    }

                    if (iterator.eventPose != null)
                    {
                        var pose = *(PxContactPairPose2*)iterator.eventPose;
                        pairs[iterator.contactPairIndex].Item0.Pose = pose.globalPose1.ToPose3();
                        pairs[iterator.contactPairIndex].Item1.Pose = pose.globalPose2.ToPose3();
                    }
                }
            }

            actor1.OnContact(actor2, 1, pairs);

            actor2.OnContact(actor1, 0, pairs);
        }

        public unsafe void CreateScene(Vector3 gravity)
        {
            var sceneDesc = PxSceneDesc_new(_physics->GetTolerancesScale());

            sceneDesc.gravity = gravity;
            sceneDesc.cpuDispatcher = (PxCpuDispatcher*)_dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();
            sceneDesc.solverType = PxSolverType.Pgs;
            sceneDesc.contactModifyCallback = _contactModify.Handle;
            sceneDesc.simulationEventCallback = _eventCallbacks.Handle;
            sceneDesc.flags = PxSceneFlags.EnableCcd | PxSceneFlags.EnablePcm | PxSceneFlags.EnableEnhancedDeterminism;
            sceneDesc.EnableCustomFilterShader(&FilterShader, 1);

            _scene = new PhysicsScene(_physics->CreateSceneMut(&sceneDesc), this);

            if (_pvd != null)
            {
                var pvdClient = _scene!.PvdClient;

                if (pvdClient != null)
                {
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitConstraints, true);
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitContacts, true);
                    pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitScenequeries, true);
                }
            }
        }


        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static unsafe PxFilterFlags FilterShader(FilterShaderCallbackInfo* info)
        {
            var actor1 = Current!._actors[info->filterData0.word0];
            var actor2 = Current!._actors[info->filterData1.word0];

            if (actor1.NotifyContacts || actor2.NotifyContacts)
                info->pairFlags[0] |=
                    PxPairFlags.SolveContact |
                    PxPairFlags.NotifyTouchFound |
                    PxPairFlags.DetectCcdContact |
                    PxPairFlags.DetectDiscreteContact |
                    PxPairFlags.NotifyTouchCcd |
                    PxPairFlags.NotifyContactPoints |
                    PxPairFlags.PostSolverVelocity |
                    PxPairFlags.PreSolverVelocity |
                    PxPairFlags.ContactEventPose;

            return 0;
        }

        public void Create(PhysicsOptions options)
        {
            _foundation = physx_create_foundation();
            _logger = options.Logger;

            if (options.DebugHost != null && options.UseDebug)
            {
                _pvd = _foundation->PhysPxCreatePvd();

                fixed (byte* bytePointer = Encoding.UTF8.GetBytes(options.DebugHost))
                {
                    var transport = phys_PxDefaultPvdSocketTransportCreate(bytePointer, options.DebugPort, 10000);
                    _pvd->ConnectMut(transport, PxPvdInstrumentationFlags.All);
                }
            }
            else
                _pvd = null;

            _tolerancesScale = new PxTolerancesScale
            {
                length = options.LengthTolerance,
                speed = options.SpeedTolerance
            };

            var curScale = _tolerancesScale;

            _physics = phys_PxCreatePhysics(
                VersionNumber,
                _foundation,
                &curScale,
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

        public void Simulate(float deltaSecs, float stepSizeSecs)
        {
            uint error;
            float curTime = 0;

            while (curTime < deltaSecs)
            {
                if (curTime + stepSizeSecs > deltaSecs)
                    _lastDeltaTime = deltaSecs - curTime;
                else
                    _lastDeltaTime = stepSizeSecs;

                _scene!.Scene.SimulateMut(_lastDeltaTime, null, null, 0, true);

                _scene.Scene.FetchResultsMut(true, &error);

                curTime += stepSizeSecs;
            }
        }

        public void Dispose()
        {
            _contactModify.Dispose();
            _eventCallbacks.Dispose();

            GC.SuppressFinalize(this);
        }

        public void DeleteObject<T>(PhysicsObject<T> obj) where T : unmanaged
        {
            _objects.Remove(new nint(obj.Handle));

            if (obj is PhysicsMaterial mat)
                _materials.Remove(mat);

            else if (obj is PhysicsActor act)
            {
                _scene!.RemoveActor(act);
                _actors.Remove(act.Id);
            }
        }

        public T GetObject<T>(void* handle)
        {
            return (T)_objects[new nint(handle)];
        }

        public float LastDeltaTime => _lastDeltaTime;


        public event Action<PhysicsActor, PhysicsActor>? Contact;

        public ref PxPhysics Physics => ref Unsafe.AsRef<PxPhysics>(_physics);

        public PhysicsScene Scene => _scene ?? throw new NullReferenceException();

        public static PhysicsSystem? Current { get; internal set; }

    }
}
