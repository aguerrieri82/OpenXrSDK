using XrEngine;

namespace XrEditor
{
    public class ComponentsSource : BaseItemsSource<TypeInfo, TypeInfo>
    {
        readonly IComponentHost _target;

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
            IEnumerable<TypeInfo> comps = TypeUtils.GetTypes(typeof(IComponent));
            return comps.Where(a =>
            {
                Type? spec = a.Type.GetInterfaces()
                                .FirstOrDefault(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IComponent<>));
                if (spec != null)
                {
                    Type arg = spec.GetGenericArguments()[0];
                    return arg.IsAssignableFrom(_target.GetType());
                }

                return true;
            });
        }

    }
}
