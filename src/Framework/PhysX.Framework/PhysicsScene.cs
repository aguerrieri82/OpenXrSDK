using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using PhysX;
using static PhysX.NativeMethods;


namespace PhysX.Framework
{
    
    public unsafe class PhysicsScene : PhysicsObject<PxScene>
    {
        public PhysicsScene(PxScene* scene, PhysicsSystem system)
            : base(scene, system)
        {

        }


        public void AddActor(PhysicsActor actor)
        {
            _handle->AddActorMut(actor.Handle, null);
        }

        public void AddActor(PhysicsActor actor, PxBVH pxBVH)
        {
            _handle->AddActorMut(actor.Handle, &pxBVH);
        }

        public void RemoveActor(PhysicsActor actor)
        {
            _handle->RemoveActorMut(actor.Handle, true);
        }

        public void SetFlag(PxSceneFlag flag, bool isSet)
        {
            _handle->SetFlagMut(flag, isSet);
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }


        public PxSceneFlags Flags
        {
            get => _handle->GetFlags();
        }



        public PxPvdSceneClient* PvdClient => _handle->GetScenePvdClientMut();

        public ref PxScene Scene => ref Unsafe.AsRef<PxScene>(_handle);
    }
}
