using Silk.NET.OpenXR;
using System.Runtime.CompilerServices;

namespace OpenXr.Framework
{
    public static class StructChain
    {
        public static unsafe void* FindNextStruct<T>(ref T obj, StructureType type) where T : unmanaged
        {
            fixed (void* ptr = &obj)
            {
                var objPtr = (BaseInStructure*)ptr;

                while (objPtr->Next != null)
                {
                    objPtr = objPtr->Next;
                    if (objPtr->Type == type)
                        return (T*)objPtr;
                }
            }

            return null;
        }

        public static unsafe void AddNextStruct<T>(ref T obj, void* next) where T : unmanaged
        {
            var nextBase = (BaseInStructure*)next;  
            fixed (void* ptr = &obj)
            {
                var objPtr = (BaseInStructure*)ptr;

                while (objPtr->Next != null && objPtr->Next->Type != nextBase->Type)
                    objPtr = objPtr->Next;

                objPtr->Next = nextBase;
            }
        }
    }
}
