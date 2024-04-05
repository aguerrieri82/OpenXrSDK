﻿namespace CanvasUI
{
    public struct UiDefaultPointer : IUiPointer
    {
        public UiDefaultPointer(int id)
        {
            Id = id;
        }

        public readonly void Capture(UiElement element)
        {
            UiManager.SetPointerCapture(Id, element);
        }

        public readonly void Release()
        {
            UiManager.SetPointerCapture(Id, null);
        }

        public int Id { get; }

    }
}
