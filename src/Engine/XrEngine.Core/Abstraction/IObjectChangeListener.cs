namespace XrEngine
{
    public interface IObjectChangeListener
    {
        void NotifyChanged(Object3D sender, ObjectChange change);
    }
}
