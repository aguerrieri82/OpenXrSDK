using System.Collections.ObjectModel;
using Xr.Test;
using XrEditor.Helpers;
using XrEngine;
using XrEngine.OpenXr;


namespace XrEditor
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
        public MainView(GraphicDriver driver)
        {
            Context.Implement<IUserInteraction>(this);

            var surface = ((IRenderSurfaceProvider)Platform.Current!).CreateRenderSurface(driver);

            Outline = new OutlinePanel();

            PropertiesEditor = new PropertiesEditor();

            SceneView = new SceneView(surface);
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

        public OutlinePanel Outline { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
