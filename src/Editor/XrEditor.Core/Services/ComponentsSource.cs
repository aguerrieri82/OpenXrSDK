using XrEngine;

namespace XrEditor
{
    public class ComponentsSource : BaseItemsSource<TypeInfo, TypeInfo>
    {
        IComponentHost _target;

        public ComponentsSource(IComponentHost target)
        {
            _target = target;   
        }

        public override string? GetText(TypeInfo item)
        {
            return item.Name;
        }

        protected override IEnumerable<TypeInfo> GetItems()
        {
            var comps = TypeUtils.GetTypes(typeof(IComponent));
            return comps.Where(a =>
            {
                var spec = a.Type.GetInterfaces()
                                .FirstOrDefault(a=> a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IComponent<>));  
                if (spec != null)
                {
                    var arg = spec.GetGenericArguments()[0];    
                    return arg.IsAssignableFrom(_target.GetType());
                }

                return true;
            });
        }

    }
}
