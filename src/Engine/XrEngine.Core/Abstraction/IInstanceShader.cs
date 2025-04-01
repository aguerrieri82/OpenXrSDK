using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IInstanceShader
    {
        bool NeedUpdate(Object3D model, long curVersion);

        unsafe long Update(byte* dstData, Object3D model, int drawId);

        public Type InstanceBufferType { get; }
    }

}
