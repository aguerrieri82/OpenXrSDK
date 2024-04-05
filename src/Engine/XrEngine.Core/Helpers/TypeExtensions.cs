namespace XrEngine
{
    public static class TypeExtensions
    {
        public static bool HasEmptyConstructor(this Type type)
        {
            foreach (var c in type.GetConstructors())
            {
                if (c.IsPublic && c.GetParameters().Length == 0)
                    return true;
            }

            return false;
        }
    }
}
