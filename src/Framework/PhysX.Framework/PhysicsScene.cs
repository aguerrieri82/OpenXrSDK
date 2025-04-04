﻿using System.Runtime.CompilerServices;


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

        public void SetVisualizationParameter(PxVisualizationParameter parameter, float value)
        {
            _handle->SetVisualizationParameterMut(parameter, value);
        }

        public float GetVisualizationParameter(PxVisualizationParameter parameter)
        {
            return _handle->GetVisualizationParameter(parameter);
        }

        public override void Dispose()
        {
            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
        }

        public PxSceneFlags Flags
        {
            get => _handle->GetFlags();
        }

        public uint Timestamp => _handle->GetTimestamp();

        public ref PxRenderBuffer RenderBuffer => ref Unsafe.AsRef<PxRenderBuffer>(_handle->GetRenderBufferMut());

        public PxPvdSceneClient* PvdClient => _handle->GetScenePvdClientMut();

        public ref PxScene Scene => ref Unsafe.AsRef<PxScene>(_handle);
    }
}
