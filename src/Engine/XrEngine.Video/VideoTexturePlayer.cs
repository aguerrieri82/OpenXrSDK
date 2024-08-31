using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Video
{
    public class VideoTexturePlayer : Behavior<Object3D>
    {
        protected VideoDecoder? _decoder;
        protected TextureData _data;
        protected double _lastFrameTime;

        public VideoTexturePlayer()
        {
            _data = new TextureData();  
        }

        protected override void Start(RenderContext ctx)
        {
            if (SrcFileName == null)
                return;

            _decoder?.Dispose();
            _decoder = new VideoDecoder();
            _decoder.Open(SrcFileName);
        }

        protected override void Update(RenderContext ctx)
        {
            if (Texture == null)
                return;

            if (_lastFrameTime == 0 || (ctx.Time - _lastFrameTime) >= 1.0 / _decoder!.FrameRate)
            {
                if (_decoder!.TryDecodeNextFrame(_data))
                {
                    Texture.Data = [_data];
                    Texture.Width = _data.Width;
                    Texture.Height = _data.Height;
                    Texture.Version++;
                    _lastFrameTime = ctx.Time;
                }
            }
        }


        public string? SrcFileName { get; set; } 

        public Texture2D? Texture { get; set; }
    }
}
