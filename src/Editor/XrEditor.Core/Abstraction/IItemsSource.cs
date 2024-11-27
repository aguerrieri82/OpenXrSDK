using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public interface IItemsSource
    {

        IEnumerable<object> Filter(string? query);

        string? GetText(object? item);

        object? GetValue(object? item); 

    }
}
