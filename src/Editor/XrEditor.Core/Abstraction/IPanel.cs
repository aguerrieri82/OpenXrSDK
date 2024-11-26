namespace XrEditor
{

    [AttributeUsage(AttributeTargets.Class)]
    public class PanelAttribute : Attribute
    {
        public PanelAttribute(string panelId)
        {
            PanelId = Guid.Parse(panelId);
        }

        public Guid PanelId { get; }
    }



    public interface IPanel
    {
        Task CloseAsync();

        void Attach(IPanelContainer container);

        bool IsActive { get; set; }

        Guid PanelId { get; }

        string? Title { get; }

        ToolbarView? ToolBar { get; }

        IPanelContainer? Container { get; }
    }
}
