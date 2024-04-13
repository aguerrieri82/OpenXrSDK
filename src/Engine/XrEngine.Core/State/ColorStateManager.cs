using XrMath;

namespace XrEngine
{
    public class ColorStateManager : ITypeStateManager<Color>
    {
        ColorStateManager() { }

        public Color Read(string key, Color curObj, Type objType, IStateContainer container)
        {
            return Color.Parse(container.Read<string>(key));
        }

        public void Write(string key, Color obj, IStateContainer container)
        {
            container.Write(key, obj.ToHex());
        }

        public static readonly ColorStateManager Instance = new();
    }
}
