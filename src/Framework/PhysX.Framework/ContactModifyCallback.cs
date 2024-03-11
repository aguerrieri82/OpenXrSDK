using PhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PhysX.Framework
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct PxContactModifyPair2
    {
        public PxRigidActor* actor1;
        public PxRigidActor* actor2;
        public PxShape* shape1;
        public PxShape* shape2;
        public PxTransform transform1;
        public PxTransform transform2;
        public PxContactSet contacts;
    }


    public unsafe class ContactModifyCallback : IDisposable
    {
        PxContactModifyCallback* _handler;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void OnContactModifyDelegate(nint thisRef, PxContactModifyPair2* pairs, uint count);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void VoidDelegate(nint thisRef);

        public ContactModifyCallback()
        {
            _handler = (PxContactModifyCallback*)Marshal.AllocHGlobal(sizeof(PxSimulationEventCallback));

            _handler->vtable_ = (void*)Marshal.AllocHGlobal(2 * sizeof(nint));

            ((nint*)_handler->vtable_)[0] = Marshal.GetFunctionPointerForDelegate((OnContactModifyDelegate)OnContactModify);
            ((nint*)_handler->vtable_)[1] = Marshal.GetFunctionPointerForDelegate((VoidDelegate)Distructor);
        }


        protected static void OnContactModify(nint thisRef, PxContactModifyPair2* pairs, uint count)
        {
        }

        protected static void Distructor(nint thisRef)
        {
        }

        public void Dispose()
        {
            if (_handler != null)
            {
                Marshal.FreeHGlobal(new nint(_handler->vtable_));
                Marshal.FreeHGlobal(new nint(_handler));
                _handler = null;
            }

            GC.SuppressFinalize(this);
        }

        public PxContactModifyCallback* Handle => _handler;
    }
}
