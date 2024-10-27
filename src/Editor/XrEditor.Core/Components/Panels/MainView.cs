using System.Collections.ObjectModel;
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

            var surface = ((IRenderSurfaceProvider)XrPlatform.Current!).CreateRenderSurface(driver);

            Context.Implement(surface);

            Outline = new OutlinePanel();

            PropertiesEditor = new PropertiesEditor();

            SceneView = new SceneView(surface);

            Toolbar = new MainToolbarView();    

            Log = new LogPanel();
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

        public MainToolbarView Toolbar { get; }

        public ObservableCollection<MessageView> Messages { get; } = [];

        public SceneView SceneView { get; }

        public OutlinePanel Outline { get; }

        public LogPanel Log { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
