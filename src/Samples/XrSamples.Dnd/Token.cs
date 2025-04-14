
using CanvasUI;
using Silk.NET.OpenGL;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.UI;
using XrMath;

namespace XrSamples.Dnd
{
    public class Token : Group3D
    {
        Group3D _tokenSet;

        public class TokenPicture : CanvasView3D
        {
            SKBitmap? _image;
            VttToken? _vttToken;
            bool _isDirty;
            SKFont? _font1;
            SKFont? _font2;
            SKPaint? _white;

            public TokenPicture()
            {
                _dpi = UnitConv.InchesToMeter * 256;
                Size = new Size2(1f, 1f);

            }

            internal void Update(VttToken vttToken)
            {
                _vttToken = vttToken;
                _isDirty = true;

            }

            protected override void Start(RenderContext ctx)
            {
                
                base.Start(ctx);
            }

            protected override void Draw(SKCanvas canvas)
            {
                canvas.Clear();
                
                _image ??= ((DndScene?)Scene)!.VttClient.DownloadImageAsync(_vttToken!.Imgsrc!).Result;

                var height = (int)(PixelSize.Height * 0.1);
                var barHeight = (int)(PixelSize.Height * 0.05);

                canvas.Save();


                if (_vttToken!.TokenStyleSelect == "circle")
                {
                    using var path = new SKPath();
                    var radius = height / 2;
                    path.AddOval(new SKRect(0, 0, PixelSize.Width, PixelSize.Height - height - barHeight));
                    canvas.ClipPath(path);
                }

                canvas.DrawBitmap(_image, new SKRect(0, 0, PixelSize.Width, PixelSize.Height - height - barHeight));

                canvas.Restore();

                if (_font1 == null)
                {
                    using var tf = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
                    _font1 = new SKFont(tf, height);
                    _font2 = new SKFont(tf, barHeight - 2);
                }

                if (_white == null)
                {
                    _white = new SKPaint();
                    _white.Color = SKColor.Parse("FFFFFF");
                }

                var size = _font1.MeasureText(_vttToken!.Name);

                canvas.DrawText(_vttToken!.Name,(_pixelSize.Width - size) / 2, _pixelSize.Height - _font1.Metrics.Descent, _font1, _white);

                var max = int.Parse(_vttToken.HitPointInfo?.Maximum?.ToString() ?? "0");
                var cur = _vttToken.HitPointInfo?.Current ?? 0;

                using var color = new SKPaint()
                {
                    Color = SKColor.Parse("1A6AFF")
                };

                var barWidth = _pixelSize.Width * (cur / (float)max);

                canvas.DrawRect(0, _pixelSize.Height - height - barHeight, barWidth, barHeight, color);
                var hp = $"{cur} / {max}";
                size = _font2!.MeasureText(hp);

                canvas.DrawText(hp, (barWidth - size) / 2, _pixelSize.Height - height - _font2.Metrics.Bottom, _font2, _white);

                _isDirty = false;

                base.Draw(canvas);
            }

            public override bool NeedDraw => _isDirty;

        }

        TokenPicture _picture;
        TriangleMesh _box;
        Object3D _mesh;
        private VttToken? _vttToken;

        public Token(Object3D mesh)
        {
            _tokenSet = new Group3D();

            _picture = new TokenPicture();
            _picture.Transform.Position = new Vector3(0, 0, 0.006f);

            _box = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#0075FF3E"));
            _box.Materials[0].Alpha = AlphaMode.Blend;

            _box.Transform.Scale = new Vector3(1, 1, 0.01f);

            _tokenSet.AddChild(_box);
            _tokenSet.AddChild(_picture);

            _mesh = mesh;


            AddChild(_tokenSet);
            AddChild(_mesh);


            _mesh.AddComponent<BoundsGrabbable>();

            var curPose = _mesh.GetWorldPose();
            _mesh.SetWorldPose(new Pose3
            {
                Orientation = curPose.Orientation,
                Position = Vector3.Zero
            });

            WorldPosition = curPose.Position;

            var y = _mesh.WorldBounds.Max.Y;
            _tokenSet.Transform.Position = new Vector3(0, y + 0.2f, 0); 
        }

        public override void Update(RenderContext ctx)
        {
            var camera = ctx.Camera!;
            var forward = camera.Forward;
            _tokenSet.Forward = new Vector3(forward.X, 0, forward.Z).Normalize();
            base.Update(ctx);
        }

        protected void UpdatePosition()
        {
            var scene = (DndScene)_scene!;
            var padding = 200;
            var mapSize = new Vector2(scene.VttScene!.Width - padding * 2.2f, scene.VttScene.Height - padding * 2.2f);
            var pos = new Vector2(int.Parse(_vttToken!.Left![..^2]), int.Parse(_vttToken.Top![..^2]));
            pos -= new Vector2(padding, padding);

            var tiles = scene.Map!.Children.OfType<Group3D>().First(a => a.Name == "Tiles");
            tiles.UpdateBounds();
            var bounds = tiles.LocalBounds;

            var localPos = pos / mapSize * new Vector2(bounds.Size.X, bounds.Size.Z);
            Transform.Position = new Vector3(localPos.X + 0.5f, Transform.Position.Y, - (bounds.Size.Z - localPos.Y - 0.5f));
        }

        public VttToken? VttToken
        {
            get => _vttToken;
            set
            {
                _vttToken = value;
                if (value != null)
                {
                    _picture.Update(value);
                    UpdatePosition();
                }

            }
        }

    }
}
