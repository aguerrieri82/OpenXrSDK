namespace XrEngine
{
    public interface IObjectPicker
    {
        Task<Collision> PickAsync(Func<Collision, bool> selector);
    }
}
