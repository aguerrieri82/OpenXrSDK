using CanvasUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrSamples
{
    public static class SettingsUIBuilder
    {
        public static IUiBuilder<T> AddInput<T, TValue>(this IUiBuilder<T> builder, string label, IInputElement<T> input) where T: UiContainer
        {
             builder
            .BeginColumn()
                .AddText(label)
                .AddChild((UiElement)input)
            .EndChild();

            return builder;
        }
    }
}
