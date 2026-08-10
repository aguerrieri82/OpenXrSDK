using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XrEngine
{
    public class StatsEmitter : Behavior<Scene3D>
    {
        protected long _lastEmissionTime;

        protected override void Update(RenderContext ctx)
        {
            if (Stopwatch.GetElapsedTime(_lastEmissionTime).TotalSeconds > 2)
            {
                Log.Warn(this, "FPS: {0} ({1})", _host.App!.Stats.Fps, _host.App!.Stats.Frame);

                _lastEmissionTime = Stopwatch.GetTimestamp();
            }
        }
    }
}
