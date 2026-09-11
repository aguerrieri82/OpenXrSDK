using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework
{
    public readonly record struct TextInputEvent(
        TextInputEventType Type,
        string? Text = null);

    public enum TextInputEventType
    {
        CommitText,
        Backspace,
        Enter
    }

    public interface ITextInputProvider
    {
        event Action<TextInputEvent>? Input;

        bool IsVisible { get; }

        void Show();
        void Hide();

        void SetText(string? text);
    }
}
