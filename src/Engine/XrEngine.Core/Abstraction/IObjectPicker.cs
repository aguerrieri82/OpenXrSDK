using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IObjectPicker
    {
        Task<Collision> PickAsync(Func<Collision, bool> selector);
    }
}
