﻿using System.Numerics;

namespace OpenXr.Engine
{
    public interface IFeatureList
    {
        void AddFeature(string name);

        void AddExtension(string name);
    }
}