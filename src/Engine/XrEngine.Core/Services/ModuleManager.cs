﻿using System.Reflection;

namespace XrEngine.Services
{
    public class ModuleManager
    {
        readonly Dictionary<Assembly, IModule?> _loaded = [];

        public ModuleManager()
        {
        }

        public void Init()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            foreach (var assemblyRef in Assembly.GetEntryAssembly()!.GetReferencedAssemblies())
                LoadAssembly(Assembly.Load(assemblyRef));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                LoadAssembly(assembly);
        }

        public void LoadAssembly(Assembly assembly)
        {
            if (_loaded.ContainsKey(assembly))
                return;

            IModule? module = null;

            var moduleAttr = assembly.GetCustomAttribute<ModuleAttribute>();
            if (moduleAttr != null)
            {
                module = (IModule)Activator.CreateInstance(moduleAttr.ModuleType)!;
                module.Load();
            }

            _loaded[assembly] = module;
        }

        private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            LoadAssembly(args.LoadedAssembly);
        }

        public static readonly ModuleManager Instance = new();
    }
}
