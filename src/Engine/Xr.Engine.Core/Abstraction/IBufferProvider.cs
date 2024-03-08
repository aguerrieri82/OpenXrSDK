﻿namespace Xr.Engine
{
    public interface IBufferProvider
    {
        IBuffer GetBuffer<T>(string name, T data, bool isGlobal);
    }
}
