using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.OpenXr
{
    public interface IPosePredictor
    {
        Pose3 Predict(float dt);

        void Track(Pose3 pose, float time);
    }
}
