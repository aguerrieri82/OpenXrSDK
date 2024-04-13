using System.Net.NetworkInformation;

namespace XrEngine
{
    public partial class TypeStateManager
    {
        readonly List<ITypeStateManager> _types = [];

        public TypeStateManager()
        {

        }

        public ITypeStateManager? Get(Type type)
        {
            return _types.FirstOrDefault(a => a.CanHandle(type));
        }

        public void Register(ITypeStateManager value)
        {
            _types.Add(value);
        }


        public static readonly TypeStateManager Instance = new();
    }
}
