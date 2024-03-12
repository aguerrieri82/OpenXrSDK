﻿namespace XrEngine
{
    public class TypeLayer<T> : BaseAutoLayer<T> where T : Object3D
    {
        protected override bool BelongsToLayer(T obj)
        {
            return true;
        }
    }
}