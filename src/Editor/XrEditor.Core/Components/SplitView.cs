using XrEngine;

namespace XrEditor
{
    public enum SplitViewMode
    {
        Horizontal,
        Vertical
    }

    public enum SplitViewSizeMode
    {
        First,
        Second
    }

    public class SplitView : BaseView, IStateManager
    {
        private BaseView? _first;
        private BaseView? _second;
        private float _size;

        public SplitView()
        {

        }

        public SplitView(SplitViewMode mode, BaseView? left = null, BaseView? right = null)
        {
            First = left;
            Second = right;
            Mode = mode;
        }

        public SplitViewMode Mode { get; set; } = SplitViewMode.Horizontal;

        public BaseView? First
        {
            get => _first;
            set
            {
                if (_first == value)
                    return;
                _first = value;
                OnPropertyChanged(nameof(First));
            }
        }

        public BaseView? Second
        {
            get => _second;
            set
            {
                if (_second == value)
                    return;
                _second = value;
                OnPropertyChanged(nameof(Second));
            }
        }

        public float Size
        {
            get => _size;
            set
            {
                if (_size == value)
                    return;
                _size = value;
                OnPropertyChanged(nameof(Size));
            }
        }


        public SplitViewSizeMode SizeMode { get; set; }

        public void GetState(IStateContainer container)
        {
            container.Write("Mode", Mode);
            container.Write("Size", Size);
            container.Write("SizeMode", SizeMode);
            if (First != null)
                container.WriteTypedObject("First", (IStateManager)First);
            if (Second != null)
                container.WriteTypedObject("Second", (IStateManager)Second);
        }

        public void SetState(IStateContainer container)
        {
            Mode = container.Read<SplitViewMode>("Mode");
            Size = container.Read<float>("Size");
            SizeMode = container.Read<SplitViewSizeMode>("SizeMode");
            First = (BaseView?)container.CreateTypedObject<IStateManager>("First");
            Second = (BaseView?)container.CreateTypedObject<IStateManager>("Second");
        }
    }
}
