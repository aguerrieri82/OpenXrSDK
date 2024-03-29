using Microsoft.VisualBasic;
using System.ComponentModel;

namespace XrEngine
{
    public class StateContext
    {
    }

    public interface IStateManager
    {
        void GetState(StateContext ctx, IStateContainer container);

        void SetState(StateContext ctx, IStateContainer container);
    }

}
