namespace Xr.Engine
{
    public static class EnumerableExtensions
    {


        public static void ForeachSafe<T>(this IEnumerable<T> target, Action<T> action)
        {
            foreach (var item in target)
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
