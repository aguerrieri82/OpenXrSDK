namespace XrEngine
{
    public class ResolveController : Behavior<Scene3D>, INotifyPropertyChangedReceiver
    {
        public void OnPropertyChanged(string name, object? value)
        {
            if (Context.TryRequire<IToneMapper>(out var mapper))
            {
                mapper.ToneMap = ToneMap;
                mapper.ResolveAlpha = ResolveAlpha;
                mapper.EncodeSrgb = EncodeSrgb;
            }
        }
        protected override void Start(RenderContext ctx)
        {
            if (Context.TryRequire<IToneMapper>(out var mapper))
            {
                ToneMap = mapper.ToneMap;
                ResolveAlpha = mapper.ResolveAlpha;
                EncodeSrgb = mapper.EncodeSrgb;
            }
        }

        public ToneMapMode ToneMap { get; set; }

        public bool ResolveAlpha { get; set; }

        public bool EncodeSrgb { get; set; }

    }
}