using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor.Abstraction
{
    public interface IItemView
    {
        string DisplayName {  get; }    
        
        object Icon { get; }
    }
}
