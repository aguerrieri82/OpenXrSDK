using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.OpenXr;
using XrSamples.Earth;

namespace XrSamples
{
    public static class Builder
    {
        public static XrEngineAppBuilder CreateEarth(this XrEngineAppBuilder builder)
        {
            var app = new EngineApp();
            app.OpenScene(new EarthScene()); 

            return builder.UseApp(app);
        }
    }
}
