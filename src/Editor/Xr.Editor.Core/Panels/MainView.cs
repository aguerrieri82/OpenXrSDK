using OpenXr.Framework;
using OpenXr.Samples;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Xr.Editor.Helpers;
using Xr.Engine;
using Xr.Engine.OpenXr;


namespace Xr.Editor
{

    public class MessageView
    {
        readonly MainView _host;

        public MessageView(MainView host, string text, string color)
        {
            CloseCommand = new Command(Close);
            _host = host;
            Text = text;
            Color = color;
        }

        public void Close()
        {
            _host.Messages.Remove(this);
        }

        public string Color { get; set; }

        public string Text { get; set; }

        public Command CloseCommand { get; }
    }


    public class MainView : BaseView, IUserInteraction
    {

        private EngineApp _app;

        public MainView(IRenderSurface surface)
        {
            Context.Implement<IUserInteraction>(this);

            SceneView = new SceneView(surface);

            PropertiesEditor = new PropertiesEditor();

            Platform.Current = new Platform
            {
                AssetManager = new LocalAssetManager(".")
            };

            LoadScene();
        }


        [MemberNotNull(nameof(_app))]
        public void LoadScene()
        {
            _app = SampleScenes.CreateChess(new LocalAssetManager("Assets"));

            _app.ActiveScene!.AddChild(new OculusSceneModel());

            var mesh = _app.ActiveScene!.FindByName<Object3D>("mesh");
            if (mesh != null)
                PropertiesEditor.ActiveObject = mesh;


            var quad = _app.ActiveScene!.FindByName<Object3D>("quad");
            quad?.AddComponent(new FollowCamera { Offset = new Vector3(0, 0, -2) });

            _app.Start();

            SceneView.Scene = _app.ActiveScene;
        }

        public void NotifyMessage(string message, MessageType type, int showTimeMs = 2000)
        {
            Context.Require<IMainDispatcher>().ExecuteAsync(async () =>
            {
                var color = type switch
                {
                    MessageType.Error => "red",
                    MessageType.Info => "blue",
                    _ => throw new NotSupportedException()
                };

                var msg = new MessageView(this, message, color);

                Messages.Add(msg);

                await UIUtils.DelayAsync(TimeSpan.FromMilliseconds(showTimeMs));

                msg.Close();

            });
        }

        public ObservableCollection<MessageView> Messages { get; } = [];

        public SceneView SceneView { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
