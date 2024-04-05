using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public class XrInputStateManager : ITypeStateManager<IXrInput>
    {
        public IXrInput Read(string key, Type objType, IStateContainer container)
        {
            var name = container.Read<string>(key);
            return XrApp.Current!.Inputs[name];
        }

        public void Write(string key, IXrInput obj, IStateContainer container)
        {
            container.Write(key, obj.Name);
        }
    }
}
