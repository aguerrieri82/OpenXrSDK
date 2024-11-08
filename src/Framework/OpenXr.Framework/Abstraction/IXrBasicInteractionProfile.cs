using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public interface IXrBasicInteractionProfile
    {
         XrInteractionProfileHand<XrInteractionProfileHandLeft>? Left { get; }

         XrInteractionProfileHand<XrInteractionProfileHandRight>? Right { get; }
    }
}
