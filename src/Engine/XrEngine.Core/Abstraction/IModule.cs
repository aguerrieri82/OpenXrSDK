namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleAttribute : Attribute
    {
        public ModuleAttribute(Type moduleType)
        {
            ModuleType = moduleType;
        }

        public Type ModuleType { get; }
    }

    public interface IModule
    {
        void Load();

        void Shutdown();    
    }
}
