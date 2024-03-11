using PhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Physics
{

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct PxContactPair2
    {
        public PxShape* shape1;
        public PxShape* shape2;
        public byte* contactPatches;
        public byte* contactPoints;
        public float* contactImpulses;
        public uint requiredBufferSize;
        public byte contactCount;
        public byte patchCount;
        public ushort contactStreamSize;
        public PxContactPairFlags flags;
        public PxPairFlags events;
        public fixed uint internalData[2];
        public fixed byte structgen_pad0[4];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct PxContactPairHeader2
    {
        public PxActor* actor1;
        public PxActor* actor2;
        public byte* extraDataStream;
        public ushort extraDataStreamSize;
        public PxContactPairHeaderFlags flags;
        public fixed byte structgen_pad0[4];
        public PxContactPair2* pairs;
        public uint nbPairs;
        public fixed byte structgen_pad1[4];
    }

    public unsafe class SimulationEventCallbacks : IDisposable
    {
        PxSimulationEventCallback* _callbacks;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void NotHandledDelegate(nint thisRef, nint p1, nint p2, nint p3);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void OnContactDelegate(nint thisRef, PxContactPairHeader2 header, PxContactPair2* pairs, int count);

        public SimulationEventCallbacks()
        {
            _callbacks = (PxSimulationEventCallback*)Marshal.AllocHGlobal(sizeof(PxSimulationEventCallback));

            _callbacks->vtable_ = (void*)Marshal.AllocHGlobal(7 * sizeof(nint));

            ((nint*)_callbacks->vtable_)[0] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
            ((nint*)_callbacks->vtable_)[1] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
            ((nint*)_callbacks->vtable_)[2] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
            ((nint*)_callbacks->vtable_)[3] = Marshal.GetFunctionPointerForDelegate((OnContactDelegate)OnContact);
            ((nint*)_callbacks->vtable_)[4] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
            ((nint*)_callbacks->vtable_)[5] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
            ((nint*)_callbacks->vtable_)[6] = Marshal.GetFunctionPointerForDelegate((NotHandledDelegate)NotHandled);
        }

        protected static void NotHandled(nint thisRef, nint p1, nint p2, nint p3)
        {
        }

        protected static void OnContact(nint thisRef, PxContactPairHeader2 header, PxContactPair2* pairs, int count)
        {
            PhysicsSystem.Current!.NotifyContact(header);
        }

        public void Dispose()
        {
            if (_callbacks != null)
            {
                Marshal.FreeHGlobal(new nint(_callbacks->vtable_));
                Marshal.FreeHGlobal(new nint(_callbacks));
                _callbacks = null;
            }

            GC.SuppressFinalize(this);
        }

        public PxSimulationEventCallback* Handle => _callbacks;
    }
}
