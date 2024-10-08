﻿using Silk.NET.OpenAL;

namespace OpenAl.Framework
{
    public abstract class AlObject
    {
        protected AL _al;
        protected uint _handle;

        public AlObject(AL al, uint handle)
        {
            _al = al;

            _handle = handle;
        }

        public uint Handle => _handle;
    }
}
