using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.UI;
using XrEngine.UI.Components;

namespace Xr.Test
{
    public class PingPongSettings : UIRoot
    {
        public PingPongSettings()
        {
            UiBuilder.From(this).Name("main").AsColumn()
                .Style(s=> s.Padding(16).BackgroundColor("#aaa"))
            .BeginRow(s=> s.ColGap(16).FlexGrow(1)).Name("cols")
                .BeginColumn(s => s.FlexBasis(1)).Name("col-1")
                    .AddText("Scene 1", s => s.FontSize(1.5f, Unit.Em))
                    .AddCheckBox("Check 1")
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1)).Name("col-2")
                    .AddText("Scene 2")
                .EndChild()
            .EndChild()
            .AddText("Footer");
        }

    }
}
