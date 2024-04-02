using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace XrEngine
{
    public class RefTable
    {


        public readonly Dictionary<ObjectId, object> Resolved = [];

        public IStateContainer? Container;

    }

    public interface IStateContext
    {
        public RefTable RefTable { get; }
    }

    public interface IStateManager
    {
        void GetState(IStateContainer container);

        void SetState(IStateContainer container);
    }

}
