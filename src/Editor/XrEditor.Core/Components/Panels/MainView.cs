using System.Collections.ObjectModel;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;


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


    public class MainView : BaseView, IUserInteraction, IStateManager
    {
        public MainView(GraphicDriver driver)
        {
            Context.Implement<IUserInteraction>(this);

            IRenderSurface surface = ((IRenderSurfaceProvider)XrPlatform.Current!).CreateRenderSurface(driver);

            Context.Implement(surface);

            Outline = new OutlinePanel();

            PropertiesEditor = new PropertiesEditor(PropertiesEditorMode.Selection, PropertiesEditor.PROPERTIES);

            ToolPropertiesEditor = new PropertiesEditor(PropertiesEditorMode.Custom, PropertiesEditor.TOOLS);

            SceneView = new SceneView(surface);

            Toolbar = new MainToolbarView();

            Log = new LogPanel();

            PanelManager pm = Context.Require<PanelManager>();

            Plotter = pm.Panel(Guid.Parse("cf183da2-a88f-499c-bea4-b286644d4e78"))!;

            IPanel draw = pm.Panel(Guid.Parse("58cd65ac-f832-4cad-84ae-4bfcb3375c77"))!;

            IPanel loopEditor = pm.Panel(Guid.Parse("c219f3ab-392d-49f6-8afc-df69d2c6d283"))!;

            Content = new SplitView
            {
                Mode = SplitViewMode.Vertical,
                First = new SplitView
                {
                    Mode = SplitViewMode.Horizontal,
                    SizeMode = SplitViewSizeMode.Second,
                    Size = 200,
                    First = new SplitView
                    {
                        Mode = SplitViewMode.Vertical,
                        Size = 250,
                        SizeMode = SplitViewSizeMode.First,
                        First = new PanelContainer(Outline),
                        Second = new PanelContainer(SceneView, loopEditor)
                    },
                    Second = new PanelContainer(Log, Plotter, draw)
                },
                Second = new PanelContainer(PropertiesEditor, ToolPropertiesEditor),
                SizeMode = SplitViewSizeMode.Second,
                Size = 360
            };
        }

        public void NotifyMessage(string message, MessageType type, int showTimeMs = 2000)
        {
            Context.Require<IMainDispatcher>().ExecuteAsync(async () =>
            {
                string color = type switch
                {
                    MessageType.Error => "red",
                    MessageType.Info => "blue",
                    _ => throw new NotSupportedException()
                };

                MessageView msg = new MessageView(this, message, color);

                Messages.Add(msg);

                await UIUtils.DelayAsync(TimeSpan.FromMilliseconds(showTimeMs));

                msg.Close();

            });
        }

        public void GetState(IStateContainer container)
        {
            container.Write("Size", Host!.Size);
            container.Write("State", Host!.State);
            container.Write("Content", Content);

        }

        public void SetState(IStateContainer container)
        {
            Host!.Size = container.Read<Size2>("Size");
            Host!.State = container.Read<WindowState>("State");
            Content = container.Read<SplitView>("Content");
        }

        public void SaveState()
        {
            JsonStateContainer container = new JsonStateContainer();
            GetState(container);
            string json = container.AsJson();
            File.WriteAllText("layout.json", json);
        }

        public void LoadState()
        {
            if (!File.Exists("layout.json"))
                return;
            string json = File.ReadAllText("layout.json");
            JsonStateContainer container = new JsonStateContainer(json);
            SetState(container);
        }

        public MainToolbarView Toolbar { get; }

        public ObservableCollection<MessageView> Messages { get; } = [];

        public SceneView SceneView { get; }

        public OutlinePanel Outline { get; }

        public LogPanel Log { get; }

        public IPanel Plotter { get; }

        public PropertiesEditor PropertiesEditor { get; }

        public PropertiesEditor ToolPropertiesEditor { get; }

        public BaseView Content { get; internal set; }

        public IWindow? Host { get; set; }
    }
}
