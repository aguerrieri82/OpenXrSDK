namespace XrEditor
{
    public class ImageMouseMoveArgs
    {
        public int X;

        public int Y;
    }

    public class ImageView : BaseView, IPointerEvents
    {
        private NativeImage? _image;
        private float _scaleY;

        public ImageView()
        {
            ScaleY = 1;
        }

        public NativeImage? Image
        {
            get => _image;
            set
            {
                if (_image?.Native == value?.Native)
                    return;
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }


        public float ScaleY
        {
            get => _scaleY;
            set
            {
                if (_scaleY == value)
                    return;
                _scaleY = value;
                OnPropertyChanged(nameof(ScaleY));
            }
        }

        void IPointerEvents.OnMouseMove(double x, double y)
        {
            MouseMove?.Invoke(this, new ImageMouseMoveArgs
            {
                X = (int)(x * Image?.Width ?? 0),
                Y = (int)(y * Image?.Height ?? 0)
            });
        }

        public event EventHandler<ImageMouseMoveArgs>? MouseMove;
    }
}
