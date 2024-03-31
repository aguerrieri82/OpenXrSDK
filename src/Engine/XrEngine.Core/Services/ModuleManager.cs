using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class ModuleManager
    {
        public ModuleManager()
        {
        }

        public void Init()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                LoadAssembly(assembly);
        }

        public void LoadAssembly(Assembly assembly)
        {
            var moduleAttr = assembly.GetCustomAttribute<ModuleAttribute>();
            if (moduleAttr != null)
            {
                var module = (IModule)Activator.CreateInstance(moduleAttr.ModuleType)!;
                module.Load();
            }
        }

        private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            LoadAssembly(args.LoadedAssembly);
        }

        public static readonly ModuleManager Instance = new();    
    }
}
