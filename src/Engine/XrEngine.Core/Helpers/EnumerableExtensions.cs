namespace XrEngine
{
    public static class EnumerableExtensions
    {
        public static void ForeachSafe<T>(this IEnumerable<T> target, Action<T> action)
        {
            if (target is IList<T> list)
            {
                int curI = 0;
                while (curI < list.Count)
                {
                    try
                    {
                        action(list[curI]);
                    }
                    catch (Exception ex)
                    {

                    }
                    curI++;
                }
            }
            else
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
}
