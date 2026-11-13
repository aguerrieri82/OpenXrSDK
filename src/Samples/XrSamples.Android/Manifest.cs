#if __ANDROID__

using OpenXr.Framework.Oculus;

[assembly: UsesFeature(OculusFeatures.ContextualBoundarylessApp)]
[assembly: UsesFeature(OculusFeatures.ExperimentalEnabled)]

#endif