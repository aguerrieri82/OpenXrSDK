﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public static class XrAppExtensions
    {
        public static void StartEventLoop(this XrApp app, int poolPeriodMs = 50)
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    if (!app.HandleEvents())
                        break;

                    await Task.Delay(poolPeriodMs);
                }
            });

        }
    }
}
