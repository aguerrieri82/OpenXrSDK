namespace OpenXr.Framework
{

    public interface IMultiViewTarget
    {
        void SetCameraTransforms(XrCameraTransform[] eyes);
    }
}
