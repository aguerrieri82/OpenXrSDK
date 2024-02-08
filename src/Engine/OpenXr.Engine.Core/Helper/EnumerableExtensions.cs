using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public static class EnumerableExtensions
    {
        public static void Update<T>(this IEnumerable<T> target, RenderContext ctx) where T : IRenderUpdate
        {
            target.ForeachSafe(a => a.Update(ctx));
        }

        public static void ForeachSafe<T>(this IEnumerable<T> target, Action<T> action)
        {
            foreach (var item in target)
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
