namespace XrEngine.Video
{
    public class VideoTexturePlayer : Behavior<Object3D>
    {

        protected TextureData _data;
        protected double _lastFrameTime;
        protected bool _isInit;

        public VideoTexturePlayer()
        {
            _data = new TextureData();
        }

        protected override void Start(RenderContext ctx)
        {
            if (Source == null)
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    Reader ??= Context.RequireNew<IVideoReader>();
                    Reader.OutTexture = Texture;
                    Texture?.SetFlag(EngineObjectFlags.EnableDebug, false);
                    Reader.Open(Source);
                    _isInit = true;
                }
                catch (Exception ex)
                {
                    Log.Error(this, ex);
                }
            });
        }

        protected override void Update(RenderContext ctx)
        {
            if (Texture == null || !_isInit)
                return;

            if (_lastFrameTime == 0 || Reader!.FrameRate == 0 || (ctx.Time - _lastFrameTime) >= 1.0 / Reader!.FrameRate)
            {
                if (Reader!.TryDecodeNextFrame(_data))
                {
                    if (_data.Data != null && _data.Data.Size > 0)
                    {
                        Texture.Data = [_data];
                        Texture.Width = _data.Width;
                        Texture.Height = _data.Height;
                        Texture.NotifyChanged(ObjectChangeType.Render);
                    }

                    _lastFrameTime = ctx.Time;
                }
            }
        }

        public override void Reset(bool onlySelf = false)
        {
            Reader?.Close();

            base.Reset(onlySelf);
        }

        public IVideoReader? Reader { get; set; }

        public Uri? Source { get; set; }

        public Texture2D? Texture { get; set; }
    }
}
