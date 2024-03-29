using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public class TypeStateManager
    {
        public class DefaultManager : ITypeStateManager<IStateManager>
        {
            public void GetState(IStateManager obj, StateContext ctx, IStateContainer container)
            {
                obj.GetState(ctx, container);
            }

            public void SetState(IStateManager obj, StateContext ctx, IStateContainer container)
            {
               obj.SetState(ctx, container);    
            }
        }


        HashSet<ITypeStateManager> _types = [];
        
        public ITypeStateManager? Get(Type type)
        {
            return _types.FirstOrDefault(a => a.CanHandle(type));
        }

        public void Register(ITypeStateManager value)
        {
            _types.Add(value);
        }


        public static readonly TypeStateManager Instance = new();
    }
}
