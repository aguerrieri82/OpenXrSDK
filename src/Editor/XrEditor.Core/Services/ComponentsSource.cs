using XrEngine;

namespace XrEditor
{
    public class ComponentsSource : BaseItemsSource<TypeInfo, TypeInfo>
    {
        ComponentsSource()
        {
        }

        public override string? GetText(TypeInfo item)
        {
            return item.Name;
        }

        protected override IEnumerable<TypeInfo> GetItems()
        {
            return TypeUtils.GetTypes(typeof(IComponent));
        }


        public static readonly ComponentsSource Instance = new();
    }
}
