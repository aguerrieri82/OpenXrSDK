using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public abstract class BaseItemsSource<TItem, TValue> : IItemsSource
    {
        public virtual IEnumerable<TItem> Filter(string? query)
        {
            var items = GetItems();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var parts = query.ToLower().Split(' ').Select(a => a.Trim());
                items = items.Where(a => parts.All(p => (GetText(a) ?? "").ToLower().Contains(p)));
            }

            return items.OrderBy(GetText);   
        }


        protected abstract IEnumerable<TItem> GetItems();

        public virtual string? GetText(TItem item)
        {
            return item?.ToString();
        }

        public virtual TValue? GetValue(TItem item)
        {
            return (TValue?)(object?)item;
        }


        string? IItemsSource.GetText(object? item)
        {
            return GetText((TItem)item!);
        }

        object? IItemsSource.GetValue(object? item)
        {
            return GetValue((TItem)item!);
        }

        IEnumerable<object> IItemsSource.Filter(string? query)
        {
            return Filter(query).Cast<object>(); 
        }
    }
}
