using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Video
{
    public class VideoTexturePlayer : Behavior<Object3D>
    {

        protected TextureData _data;
        protected double _lastFrameTime;

        public VideoTexturePlayer()
        {
            _data = new TextureData();  
        }

        protected override void Start(RenderContext ctx)
        {
            if (Source == null)
                return;
     
            Reader ??= Context.RequireInstance<IVideoReader>();
            Reader.OutTexture = Texture;
            Reader.Open(Source);
        }

        protected override void Update(RenderContext ctx)
        {
            if (Texture == null)
                return;

            if (_lastFrameTime == 0 || Reader!.FrameRate == 0 || (ctx.Time - _lastFrameTime) >= 1.0 / Reader!.FrameRate)
            {
                if (Reader!.TryDecodeNextFrame(_data))
                {
                    if (_data.Data.Length > 0)
                    {
                        Texture.Data = [_data];
                        Texture.Version++;
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
