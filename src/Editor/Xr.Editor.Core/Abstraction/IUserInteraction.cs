using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor
{
    public enum MessageType
    {
        Info,
        Error
    }

    public interface IUserInteraction
    {
        void NotifyMessage(string message, MessageType type, int showTimeMs = 2000); 
    }
}
