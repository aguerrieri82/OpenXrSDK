
#if GLES
using Silk.NET.OpenGLES;
#else

using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Samples;
using Silk.NET.OpenXR;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Xr.Engine.Editor
{
    public class MainWindowView
    {
        RenderHost _renderHost;
        private Thread? _renderThread;

        public MainWindowView(RenderHost renderHost)
        {
            _renderHost = renderHost;

            LoadScene();
        }

        public void LoadScene()
        {
            var app = Common.CreateScene(new LocalAssetManager("Assets"));

            var camera = (app.ActiveScene?.ActiveCamera as PerspectiveCamera)!;
            camera.BackgroundColor = Color.White;   

            var view = new Rect2I();

            void UpdateSize()
            {
                view.Width = (uint)(_renderHost.RenderSize.Width * 1.25f);
                view.Height = (uint)(_renderHost.RenderSize.Height * 1.25f);
                camera.SetFov(45, view.Width, view.Height);
            }

            _renderHost.SizeChanged += (_, _) =>
            {
                UpdateSize();
            };

            camera!.LookAt(new Vector3(0f, 2f, 2f), Vector3.Zero, new Vector3(0, 1, 0));

            var render = new OpenGLRender(_renderHost.Gl!);

            //render.EnableDebug();

            app.Renderer = render;

            app.Start();

            UpdateSize();

            _renderHost.ReleaseContext();

            _renderThread = new Thread(() =>
            {
                _renderHost.TakeContext();

                while (true)
                {
                    app.RenderFrame(view);
                    _renderHost.SwapBuffers();
                }
            });

            _renderThread.Start();  

    
        }

        private void OnIdle(object? sender, EventArgs e)
        {
            
        }
    }
}
