using XrEditor.Abstraction;

namespace XrEditor
{
    public class ImageView : BaseView
    {
        private NativeImage? _image;
        private float _scaleY;

        public ImageView()
        {
            ScaleY = -1;
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

    }
}
